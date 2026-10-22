namespace Orc.Extensibility.Tests.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PluginManagerFacts
{
    [Test]
    public async Task Refresh_Preserves_Plugin_Finder_Order()
    {
        var firstPlugin = new Mock<IPluginInfo>();
        firstPlugin.SetupGet(plugin => plugin.Name).Returns("Zulu");

        var secondPlugin = new Mock<IPluginInfo>();
        secondPlugin.SetupGet(plugin => plugin.Name).Returns("Alpha");

        IReadOnlyList<IPluginInfo> plugins = new[]
        {
            firstPlugin.Object,
            secondPlugin.Object
        };

        var pluginFinder = new Mock<IPluginFinder>();
        pluginFinder.Setup(finder => finder.FindPluginsAsync()).ReturnsAsync(plugins);

        var pluginManager = new PluginManager(pluginFinder.Object);

        await pluginManager.RefreshAsync();

        var actualPlugins = pluginManager.GetPlugins();

        Assert.That(actualPlugins, Is.EqualTo(plugins));
    }
}
