using NUnit.Framework;

namespace SomeTests
{
    public class ThoseTests
    {
        [Test]
        public void TheTest()
            => Assert.That(true,
                Is.Not.False);
    }
}
