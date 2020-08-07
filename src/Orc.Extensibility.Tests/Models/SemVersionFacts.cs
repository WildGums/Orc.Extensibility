namespace Orc.Extensibility.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class SemVersionFacts
    {
        [TestCase("1.0.0-alpha.916", "1.0.0-alpha.1028", "1.0.0-alpha.1028")]
        [TestCase("1.0.0-alpha.1028", "1.0.0-beta.1", "1.0.0-beta.1")]
        public void Compare(string v1, string v2, string expectedVersion)
        {
            var version1 = new SemVersion(v1);
            var version2 = new SemVersion(v2);

            var largest = (version1 > version2) ? v1 : v2;
            var equal = string.Equals(expectedVersion, largest, StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(equal);
        }
    }
}
