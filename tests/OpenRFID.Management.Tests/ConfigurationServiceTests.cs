using OpenRFID.Core.Engine.Configuration;

namespace OpenRFID.Management.Tests;

public class ConfigurationServiceTests : IDisposable
{
    private readonly string _tempConfigFile = Path.Combine(Path.GetTempPath(), $"openrfid_test_{Guid.NewGuid()}.json");

    [Fact]
    public void LoadOrCreateDefault_CreatesDefaultConfig_WhenFileDoesNotExist()
    {
        var service = new ConfigurationService(_tempConfigFile);
        var config = service.Current;

        Assert.NotNull(config);
        Assert.NotEmpty(config.Readers);
        Assert.Equal(5.0, config.Filter.SlidingWindowSeconds);
        Assert.Equal("POST", config.Dispatch.HttpMethod);
    }

    [Fact]
    public void SaveConfig_PersistsToDisk_AndFiresConfigChangedEvent()
    {
        var service = new ConfigurationService(_tempConfigFile);
        bool eventFired = false;
        service.ConfigChanged += (sender, c) => eventFired = true;

        var updated = service.Current with
        {
            Filter = service.Current.Filter with { SlidingWindowSeconds = 15.0 }
        };

        service.SaveConfig(updated);

        Assert.True(eventFired);
        Assert.Equal(15.0, service.Current.Filter.SlidingWindowSeconds);

        // Reload from new service instance pointing to same file
        var newService = new ConfigurationService(_tempConfigFile);
        Assert.Equal(15.0, newService.Current.Filter.SlidingWindowSeconds);
    }

    [Fact]
    public void ValidateConfig_ThrowsArgumentException_OnInvalidUrl()
    {
        var config = new OpenRFIDConfig
        {
            Dispatch = new DispatchConfig { TargetUrl = "not-a-valid-url" }
        };

        Assert.Throws<ArgumentException>(() => ConfigurationService.ValidateConfig(config));
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            File.Delete(_tempConfigFile);
        }
    }
}
