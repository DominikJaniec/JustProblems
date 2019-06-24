using System;
using NUnit.Framework;

namespace NUnitInconsistency
{
    public class ExecuteVsExploreTests
    {
        private const bool ShouldCauseProblems = false;

        public static string[] ProblematicSource()
        {
            if (ShouldCauseProblems)
                throw new Exception("Problem!");

            return new[] { "First", "Omega" };
        }

        [TestCaseSource(nameof(ProblematicSource))]
        public void InconsistencyTest(string value)
            => Assert.IsNotEmpty(value, "Just for show");
    }
}
