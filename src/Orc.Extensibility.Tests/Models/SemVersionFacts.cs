namespace Orc.Extensibility.Tests;

using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class SemVersionFacts
{
    [TestCase("1.0.0-alpha.916", "1.0.0-alpha.1028", "1.0.0-alpha.1028")]
    [TestCase("1.0.0-alpha.1028", "1.0.0-beta.1", "1.0.0-beta.1")]
    [TestCase("2.8.0-beta1+1234", "2.8.0-beta1234", "2.8.0-beta1234")]
    [TestCase("2.8.0-beta1234", "2.8.0-beta1+1234", "2.8.0-beta1234")]
    [TestCase("2.8.0-beta.9+1234", "2.8.0-beta.10", "2.8.0-beta.10")]
    [TestCase("2.8.0-beta.1+9999", "2.8.0-beta.2+1", "2.8.0-beta.2+1")]
    [TestCase("2.8.0+1234", "2.8.0-beta1234", "2.8.0+1234")]
    public void Compare(string v1, string v2, string expectedVersion)
    {
        var version1 = new SemVersion(v1);
        var version2 = new SemVersion(v2);

        var largest = (version1 > version2) ? v1 : v2;
        var equal = string.Equals(expectedVersion, largest, StringComparison.OrdinalIgnoreCase);

        Assert.That(equal, Is.True);
    }

    [TestCase("2.8.0-beta1+1234", "2.8.0-beta1234")]
    [TestCase("2.8.0-beta.9+1234", "2.8.0-beta.10+1")]
    public void Sort_Prefers_Newer_Prerelease_Over_Build_Metadata(string olderVersion, string newerVersion)
    {
        var versions = new[] { new SemVersion(olderVersion), new SemVersion(newerVersion) };

        var sortedVersions = versions.OrderByDescending(version => version).ToArray();

        Assert.That(sortedVersions[0].Version, Is.EqualTo(newerVersion));
        Assert.That(sortedVersions[1].Version, Is.EqualTo(olderVersion));
    }

    [TestCase("2.8.0-beta.1+1234", "2.8.0-beta.1+1222")]
    [TestCase("2.8.0-beta1+1234", "2.8.0-beta1")]
    [TestCase("2.8.0+1234", "2.8.0+1222")]
    [TestCase("2.8.0+1234", "2.8.0")]
    public void Compare_Uses_Build_Metadata_When_Primary_Versions_Are_Equal(string firstVersion, string secondVersion)
    {
        var version1 = new SemVersion(firstVersion);
        var version2 = new SemVersion(secondVersion);

        Assert.That(version1.CompareTo(version2), Is.GreaterThan(0));
        Assert.That(version2.CompareTo(version1), Is.LessThan(0));
        Assert.That(version1 > version2, Is.True);
        Assert.That(version1 == version2, Is.False);

        var sortedVersions = new[] { version2, version1 }.OrderByDescending(version => version).ToArray();

        Assert.That(sortedVersions[0].Version, Is.EqualTo(firstVersion));
    }
}
