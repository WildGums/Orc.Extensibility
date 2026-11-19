namespace Orc.Extensibility.Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Orc.FileSystem;

public class PluginFinderBaseFacts
{
    [TestFixture]
    public class The_FindPluginsAsync_Method
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task Keeps_Highest_Version_Regardless_Of_Discovery_Order(bool newestFirst)
        {
            var older = CreatePlugin("2.8.7", @"C:\plugins\older.dll");
            var newer = CreatePlugin("2.8.13-beta03", @"C:\plugins\newer.dll");
            var other = CreatePlugin("1.0.0", @"C:\plugins\other.dll", "OtherPlugin");
            var resolver = new Mock<IRuntimeAssemblyResolverService>();
            var plugins = newestFirst ? new[] { newer, older, other } : new[] { older, newer, other };
            var finder = new TestPluginFinder(plugins, resolver.Object);

            var result = await finder.FindPluginsAsync();

            Assert.That(result, Is.EquivalentTo(new[] { newer, other }));
            resolver.Verify(x => x.UnregisterAssemblyAsync(It.Is<IRuntimeAssembly>(assembly => assembly.Source == older.Location)), Times.Once);
        }

        private static IPluginInfo CreatePlugin(string version, string location, string typeName = "ExamplePlugin")
        {
            var type = new Mock<IPluginTypeInfo>();
            type.SetupGet(x => x.FullTypeName).Returns(typeName);
            var plugin = new Mock<IPluginInfo>();
            plugin.SetupGet(x => x.Version).Returns(version);
            plugin.SetupGet(x => x.Location).Returns(location);
            plugin.SetupGet(x => x.Plugin).Returns(type.Object);
            return plugin.Object;
        }

        private class TestPluginFinder : PluginFinderBase
        {
            private readonly IReadOnlyList<IPluginInfo> _plugins;

            public TestPluginFinder(IReadOnlyList<IPluginInfo> plugins, IRuntimeAssemblyResolverService resolver)
                : base(Mock.Of<IPluginLocationsProvider>(), Mock.Of<IPluginInfoProvider>(), Mock.Of<IPluginCleanupService>(),
                    Mock.Of<IDirectoryService>(), Mock.Of<IFileService>(), Mock.Of<IAssemblyReflectionService>(), resolver)
            {
                _plugins = plugins;
            }

            protected override Task FindPluginsInLoadedAssembliesAsync(PluginProbingContext context)
            {
                context.Plugins.AddRange(_plugins);
                return Task.CompletedTask;
            }

            protected override Task FindPluginsInUnloadedAssembliesAsync(PluginProbingContext context)
            {
                return Task.CompletedTask;
            }

            protected override bool IsPlugin(PluginProbingContext context, Type type)
            {
                return true;
            }
        }
    }
}
