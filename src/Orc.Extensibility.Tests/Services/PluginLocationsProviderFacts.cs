namespace Orc.Extensibility.Tests.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel.Services;
    using NUnit.Framework;

    public class PluginLocationsProviderFacts
    {
        [TestFixture]
        public class The_GetPluginDirectories_Method
        {
            [Test]
            public async Task Does_Not_Include_Multiple_Directories()
            {
                var appDataService = new AppDataService();

                var pluginLocationsProvider = new PluginLocationsProvider(appDataService);

                var directories = pluginLocationsProvider.GetPluginDirectories();

                var count = directories.Count();
                var distinctCount = directories.Distinct().Count();

                Assert.That(count, Is.EqualTo(distinctCount));
            }
        }

        [TestFixture]
        public class The_GetPluginLocations_Method
        {
            [Test]
            public async Task Does_Not_Include_Multiple_Directories()
            {
                var appDataService = new AppDataService();

                var pluginLocationsProvider = new PluginLocationsProvider(appDataService);

                var directories = pluginLocationsProvider.GetPluginDirectories();

                var count = directories.Count();
                var distinctCount = directories.Distinct().Count();

                Assert.That(count, Is.EqualTo(distinctCount));
            }
        }
    }
}
