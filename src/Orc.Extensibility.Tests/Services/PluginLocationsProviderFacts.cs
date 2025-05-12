namespace Orc.Extensibility.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Catel.Services;
    using Moq;
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
    }
}
