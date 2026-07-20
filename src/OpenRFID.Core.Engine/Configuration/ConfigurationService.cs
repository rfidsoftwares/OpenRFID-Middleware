using System.Text.Json;

namespace OpenRFID.Core.Engine.Configuration;

/// <summary>
/// Service responsible for persisting, validating, and publishing configuration updates.
/// </summary>
public sealed class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _lock = new();
    private OpenRFIDConfig _currentConfig;

    public event EventHandler<OpenRFIDConfig>? ConfigChanged;

    public ConfigurationService(string? configFilePath = null)
    {
        _configFilePath = configFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openrfid.config.json");
        _currentConfig = LoadOrCreateDefault();
    }

    public OpenRFIDConfig Current => GetCurrentConfig();

    private OpenRFIDConfig GetCurrentConfig()
    {
        lock (_lock)
        {
            return _currentConfig;
        }
    }

    public OpenRFIDConfig LoadOrCreateDefault()
    {
        lock (_lock)
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<OpenRFIDConfig>(json, _jsonOptions);
                    if (config != null)
                    {
                        ValidateConfig(config);
                        _currentConfig = config;
                        ConfigChanged?.Invoke(this, _currentConfig);
                        return _currentConfig;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ConfigurationService] Warning: Failed to load config from {_configFilePath}: {ex.Message}. Falling back to default.");
                }
            }

            _currentConfig = GetDefaultConfig();
            SaveConfig(_currentConfig);
            return _currentConfig;
        }
    }

    public void SaveConfig(OpenRFIDConfig config)
    {
        ValidateConfig(config);

        lock (_lock)
        {
            _currentConfig = config;
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(_configFilePath, json);
        }

        ConfigChanged?.Invoke(this, config);
    }

    public static void ValidateConfig(OpenRFIDConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Filter.SlidingWindowSeconds < 0)
        {
            throw new ArgumentException("SlidingWindowSeconds cannot be negative.");
        }

        if (config.Dispatch.PeriodicIntervalMs <= 0)
        {
            throw new ArgumentException("PeriodicIntervalMs must be greater than zero.");
        }

        if (config.Dispatch.BatchCountThreshold <= 0)
        {
            throw new ArgumentException("BatchCountThreshold must be greater than zero.");
        }

        if (!Uri.IsWellFormedUriString(config.Dispatch.TargetUrl, UriKind.Absolute))
        {
            throw new ArgumentException($"TargetUrl '{config.Dispatch.TargetUrl}' is not a valid absolute URL.");
        }
    }

    private static OpenRFIDConfig GetDefaultConfig()
    {
        return new OpenRFIDConfig
        {
            Readers = [
                new Abstractions.ReaderConfig
                {
                    ReaderId = "Simulator-01",
                    ProviderId = "Simulator",
                    BrandName = "OpenRFID Virtual Simulator",
                    IpAddress = "127.0.0.1",
                    Port = 5084
                }
            ],
            Filter = new FilterConfig
            {
                SlidingWindowSeconds = 5.0,
                DailyUniqueEnabled = false,
                MinRssiDbm = -75.0f
            },
            Dispatch = new DispatchConfig
            {
                TargetUrl = "http://localhost:8080/api/v1/tags",
                HttpMethod = "POST",
                TriggerMode = "Instant"
            },
            Storage = new StorageConfig
            {
                DbPath = "openrfid_offline_queue.db",
                MaxQueueSize = 100000,
                ReplayRatePerSecond = 50
            },
            Security = new SecurityConfig
            {
                Enabled = false,
                ApiKey = "openrfid-secret-key-12345",
                EnableCors = true,
                AllowedOrigins = ["*"]
            }
        };
    }
}
