using System;

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

    public static class Worker
    {
        public static void Job(
            string name,
            (int setup, int work) times,
            Barrier barrier)
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
        public static int RNG_SEED = 120220314;

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

            var jobsCount = jobsNames.Length;

            var barrier = new Barrier(jobsCount,
                postPhaseAction: _ => Tools
                    .Log("MAIN", "Barrier breached!"));

            var jobs = new List<Task>(jobsCount);
            foreach (var jobName in jobsNames)
            {
                var setup = rng.Next(111, 444);
                var work = rng.Next(333, 777);

                jobs.Add(Task.Run(
                    () => Worker.Job(jobName,
                        (setup, work),
                        barrier)));
            }

            Task.WaitAll(jobs.ToArray());
        }
    }
}
