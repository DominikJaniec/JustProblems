using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace JustProblems.SystemWideBarrier
{
    public static class Tools
    {
        public static void Log(string tag, string message)
        {
            var now = DateTime.Now
                .ToString("yyyy-MM-dd HH:mm:ss.fff");

            Console.WriteLine($"{now}|{tag}] {message}");
        }
    }

    public class SWBarrier
    {
        private SWBarrier() { }

        private string Barrier { get; init; }
        private string Participant { get; init; }

        public void SignalAndWait()
        {
            using var barrier = Semaphore.OpenExisting(Barrier);
            using var signal = Semaphore.OpenExisting(Participant);

            signal.Release();
            barrier.WaitOne();
        }

        public static Dictionary<string, SWBarrier> CreateFor(
            string[] participantsIdentities,
            Action postPhaseAction)
        {
            var barrierName = $"{nameof(SystemWideBarrier)}_main-barrier";
            var participantPrefix = $"{nameof(SystemWideBarrier)}_part";

            var barriers = participantsIdentities
                .ToDictionary(x => x,
                    x => new SWBarrier
                    {
                        // Note: Here we should have some prefix,
                        //       and also `participant` should be
                        //       uniquely encoded - let's hope the
                        //       `GetHashCode` is good enough now.
                        Barrier = barrierName,
                        Participant = participantPrefix
                            + Math.Abs(x.GetHashCode())
                    });

            var participantsSemaphores = barriers.Values
                .Select(br => br.Participant)
                .Concat(new[] { barrierName })
                .Select(x => "\n\t* " + x);

            var semaphores = string.Concat(participantsSemaphores);
            Tools.Log("SETUP", $"Participants:" + semaphores);

            ThrowIfAlreadyExists(barrierName);
            foreach (var br in barriers.Values)
                ThrowIfAlreadyExists(br.Participant);

            ////////////////////////////////////////////////////////

            var participants = participantsIdentities.Length;
            var mainBarrier = new Semaphore(
                initialCount: 0,
                maximumCount: participants,
                name: barrierName);

            var signalers = new List<Semaphore>(participants);
            foreach (var br in barriers.Values)
                signalers.Add(
                    new Semaphore(0, 1, br.Participant));

            Task.Run(() =>
            {
                foreach (var ps in signalers)
                {
                    ps.WaitOne();
                    ps.Dispose();
                }

                postPhaseAction();

                mainBarrier.Release(participants);
                mainBarrier.Dispose();
            });

            return barriers;
        }

        private static void ThrowIfAlreadyExists(string name)
        {
            if (!Semaphore.TryOpenExisting(name, out var semaphore))
                return;

            semaphore.Dispose();
            throw new InvalidOperationException(
                $"Semaphore '{name}' is already existing.");
        }
    }

    public static class Worker
    {
        public static void Job(
            string name,
            (int setup, int work) times,
            SWBarrier barrier)
        {
            Tools.Log(name, "Starting Job...");
            FakeWork(times.setup);

            Tools.Log(name, "Barrier reached.");
            barrier.SignalAndWait();

            Tools.Log(name, "Executing work...");
            FakeWork(times.work);

            Tools.Log(name, "Everything done.");
        }

        private static void FakeWork(
            int executionMilliseconds)
            => Thread.Sleep(
                TimeSpan.FromMilliseconds(
                    executionMilliseconds));
    }

    public class Program
    {
        public static int RNG_SEED = 1202203147;

        public static void Main(string[] args)
        {
            var rng = new Random(RNG_SEED);

            var jobsNames = new[]{
                "pierwsza",
                "second",
                "tercera",
                "avant",
                "quatrième",
                "п'ятий"
            };

            var jobsBarriers = SWBarrier.CreateFor(jobsNames,
                postPhaseAction: () => Tools
                    .Log("MAIN", "Barrier breached!!"));

            var jobs = new List<Task>(jobsNames.Length);
            foreach (var jobName in jobsNames)
            {
                var setup = rng.Next(111, 444);
                var work = rng.Next(333, 777);

                jobs.Add(Task.Run(
                    () => Worker.Job(jobName,
                        (setup, work),
                        jobsBarriers[jobName])));
            }

            Task.WaitAll(jobs.ToArray());
        }
    }
}
