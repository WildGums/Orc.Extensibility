﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SemVersion.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{Version}")]
    internal class SemVersion : IComparable
    {
        private readonly System.Version _classicVersion;
        private readonly string _version;

        public SemVersion(string version)
        {
            _version = version;
            var classicVersion = StripDashPartOfVersion(version);
            _classicVersion = new System.Version(classicVersion);
        }

        public string Version
        {
            get { return _version; }
        }

        public System.Version ClassicVersion
        {
            get { return _classicVersion; }
        }

        public int CompareTo(object obj)
        {
            var otherVersion = obj as SemVersion;
            if (otherVersion == null)
            {
                return 0;
            }

            return CompareVersions(Version, otherVersion.Version);
        }

        public static bool operator >(SemVersion x, SemVersion y)
        {
            return CompareVersions(x.Version, y.Version) > 0;
        }

        public static bool operator <(SemVersion x, SemVersion y)
        {
            return CompareVersions(x.Version, y.Version) < 0;
        }

        public static bool operator >=(SemVersion x, SemVersion y)
        {
            return CompareVersions(x.Version, y.Version) >= 0;
        }

        public static bool operator <=(SemVersion x, SemVersion y)
        {
            return CompareVersions(x.Version, y.Version) <= 0;
        }

        public static bool operator ==(SemVersion x, SemVersion y)
        {
            if (ReferenceEquals(x, null) && ReferenceEquals(y, null))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            return CompareVersions(x.Version, y.Version) == 0;
        }

        public static bool operator !=(SemVersion x, SemVersion y)
        {
            return !(x == y);
        }

        public override bool Equals(object o)
        {
            try
            {
                return this == (SemVersion)o;
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return _version;
        }

        private static string GetComparableVersion(string version)
        {
            var comparableVersion = version.ToLower();

            // We have beta / unstable, etc, replace them by numbers for an easy compare
            comparableVersion = comparableVersion.Replace("unstable", "01");
            comparableVersion = comparableVersion.Replace("alpha", "02");
            comparableVersion = comparableVersion.Replace("beta", "03");
            comparableVersion = comparableVersion.Replace("rc", "04");
            comparableVersion = comparableVersion.Replace("releasecandidate", "04");

            return comparableVersion;
        }

        private static string StripDashPartOfVersion(string version)
        {
            var dashIndex = version.IndexOf('-');
            if (dashIndex != -1)
            {
                version = version.Substring(0, dashIndex);
            }

            var commaIndex = version.IndexOf(',');
            if (commaIndex != -1)
            {
                version = version.Substring(0, commaIndex);
            }

            return version;
        }

        private static int CompareVersions(string versionA, string versionB)
        {
            var originalVersionA = versionA;
            var originalVersionB = versionB;

            var versionWithoutDashPart = StripDashPartOfVersion(originalVersionA);
            var versionToCheckWithoutDashPart = StripDashPartOfVersion(originalVersionB);

            versionA = versionWithoutDashPart;
            versionB = versionToCheckWithoutDashPart;

            if (string.Equals(versionWithoutDashPart, versionToCheckWithoutDashPart))
            {
                // Without dash part, versions are equal, special care

                // If 1 of the items does not contain a dash, treat that as larger (1.0.0 is larger than 1.0.0-beta)
                if (string.Equals(originalVersionA, versionA) && !string.Equals(originalVersionB, versionB))
                {
                    return 1;
                }

                // If 1 of the items does not contain a dash, treat that as larger (1.0.0-beta is smaller than 1.0.0)
                if (!string.Equals(originalVersionA, versionA) && string.Equals(originalVersionB, versionB))
                {
                    return -1;
                }

                // Get special versions
                versionA = GetComparableVersion(originalVersionA);
                versionB = GetComparableVersion(originalVersionB);
            }

            return string.Compare(versionA, versionB);
        }
    }
}