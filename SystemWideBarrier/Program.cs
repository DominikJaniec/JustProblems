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
        public static void Work(string name, int load)
        {
            Tools.Log(name, "Starting work...");

            Thread.Sleep(
                TimeSpan.FromMilliseconds(load));

            Tools.Log(name, "Work done.");
        }
    }

    public class Program
    {
        public static int RNG_SEED = 120220314;

        public static void Main(string[] args)
        {
            var jobsNames = new[]{
                "pierwsza",
                "second",
                "tercera",
                "avant",
                "quatrième",
                "п'ятий"
            };

            var rng = new Random(RNG_SEED);
            var jobs = new List<Task>();

            foreach (var jn in jobsNames)
            {
                var load = rng.Next(333, 777);
                jobs.Add(Task.Run(
                    () => Worker.Work(jn, load)));
            }

            Task.WaitAll(jobs.ToArray());
        }
    }
}
