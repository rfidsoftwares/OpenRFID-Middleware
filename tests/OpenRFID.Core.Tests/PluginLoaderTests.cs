using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Plugins;
using Xunit;

namespace OpenRFID.Core.Tests;

public class PluginLoaderTests
{
    private class DummyProvider : IReaderProvider
    {
        public string ProviderId => "dummy-test";
        public string BrandName => "Dummy Provider";
        public IReadOnlyList<string> SupportedProtocols => new[] { "TestProtocol" };

        public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void PluginLoader_RegisterProvider_StoresAndRetrievesProvider()
    {
        var loader = new PluginLoader();
        var provider = new DummyProvider();

        bool registered = loader.RegisterProvider(provider);
        var retrieved = loader.GetProvider("DUMMY-TEST");

        Assert.True(registered);
        Assert.NotNull(retrieved);
        Assert.Equal("dummy-test", retrieved.ProviderId);
        Assert.Single(loader.Providers);
    }
}
