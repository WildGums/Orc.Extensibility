namespace Orc.Extensibility.Tests.Watchers;

using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Catel.Services;
using Moq;
using NUnit.Framework;
using FileSystem;

[TestFixture]
public class AppDomainRuntimeAssemblyWatcherFacts
{
    [Test]
    public async Task Returns_Costura_Embedded_File_Exact_Resource_Async()
    {
        var costuraRuntimeAssemblyNlMock = new Mock<ICosturaRuntimeAssembly>();
        costuraRuntimeAssemblyNlMock.Setup(x => x.RelativeFileName)
            .Returns("nl-NL/MyAssembly.resources.dll");

        var runtimeAssemblyResolverServiceMock = new Mock<IRuntimeAssemblyResolverService>();
        runtimeAssemblyResolverServiceMock.Setup(x => x.GetPluginLoadContexts())
            .Returns(() =>
            {
                var pluginLoadContextMock = new Mock<IPluginLoadContext>();
                pluginLoadContextMock.Setup(x => x.RuntimeAssemblies)
                    .Returns(() =>
                    {
                        var runtimeAssemblies = new List<IRuntimeAssembly>
                        {
                            costuraRuntimeAssemblyNlMock.Object
                        };

                        return runtimeAssemblies;
                    });

                return new IPluginLoadContext[]
                {
                    pluginLoadContextMock.Object
                };
            });

        var appDataService = new AppDataService();
        var fileService = new FileService();
        var directoryService = new DirectoryService(fileService);

        var appDomainRuntimeAssemblyWatcher = new AppDomainRuntimeAssemblyWatcher(
            runtimeAssemblyResolverServiceMock.Object,
            appDataService,
            directoryService,
            fileService);

        appDomainRuntimeAssemblyWatcher.AssemblyLoading += (sender, e) =>
        {
            Assert.That(costuraRuntimeAssemblyNlMock.Object, Is.EqualTo(e.ResolvedRuntimeAssembly));

            e.Cancel = true;
        };

        var assemblyLoadContext = new AssemblyLoadContext("test", true);
        var assemblyFullName = "MyAssembly.resources, Culture=nl-NL, Version=1.0.0.0";

        appDomainRuntimeAssemblyWatcher.LoadManagedAssembly(assemblyLoadContext,
            new System.Reflection.AssemblyName(assemblyFullName), assemblyFullName);

        assemblyLoadContext.Unload();
    }

    [Test]
    public async Task Returns_Costura_Embedded_File_Parent_Resource_Async()
    {
        var costuraRuntimeAssemblyNlMock = new Mock<ICosturaRuntimeAssembly>();
        costuraRuntimeAssemblyNlMock.Setup(x => x.RelativeFileName)
            .Returns("nl/MyAssembly.resources.dll");

        var runtimeAssemblyResolverServiceMock = new Mock<IRuntimeAssemblyResolverService>();
        runtimeAssemblyResolverServiceMock.Setup(x => x.GetPluginLoadContexts())
            .Returns(() =>
            {
                var pluginLoadContextMock = new Mock<IPluginLoadContext>();
                pluginLoadContextMock.Setup(x => x.RuntimeAssemblies)
                    .Returns(() =>
                    {
                        var runtimeAssemblies = new List<IRuntimeAssembly>
                        {
                            costuraRuntimeAssemblyNlMock.Object
                        };

                        return runtimeAssemblies;
                    });

                return new IPluginLoadContext[]
                {
                    pluginLoadContextMock.Object
                };
            });

        var appDataService = new AppDataService();
        var fileService = new FileService();
        var directoryService = new DirectoryService(fileService);

        var appDomainRuntimeAssemblyWatcher = new AppDomainRuntimeAssemblyWatcher(
            runtimeAssemblyResolverServiceMock.Object,
            appDataService,
            directoryService,
            fileService);

        appDomainRuntimeAssemblyWatcher.AssemblyLoading += (sender, e) =>
        {
            Assert.That(costuraRuntimeAssemblyNlMock.Object, Is.EqualTo(e.ResolvedRuntimeAssembly));

            e.Cancel = true;
        };

        var assemblyLoadContext = new AssemblyLoadContext("test", true);
        var assemblyFullName = "MyAssembly.resources, Culture=nl-NL, Version=1.0.0.0";

        appDomainRuntimeAssemblyWatcher.LoadManagedAssembly(assemblyLoadContext,
            new System.Reflection.AssemblyName(assemblyFullName), assemblyFullName);

        assemblyLoadContext.Unload();
    }
}
