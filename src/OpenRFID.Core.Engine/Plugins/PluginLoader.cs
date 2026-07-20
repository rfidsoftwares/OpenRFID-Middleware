using System.Collections.Concurrent;
using System.Reflection;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Engine.Plugins;

/// <summary>
/// Dynamic plugin discovery and provider factory manager.
/// </summary>
public sealed class PluginLoader
{
    private readonly ConcurrentDictionary<string, IReaderProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PluginAssemblyLoadContext> _loadContexts = new();

    public IReadOnlyCollection<IReaderProvider> Providers => _providers.Values.ToList().AsReadOnly();

    /// <summary>
    /// Explicitly registers a pre-instantiated provider instance.
    /// </summary>
    public bool RegisterProvider(IReaderProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return _providers.TryAdd(provider.ProviderId, provider);
    }

    /// <summary>
    /// Scans a directory for compiled plugin assembly files (*.dll) and instantiates all implemented IReaderProvider types.
    /// </summary>
    public int LoadPluginsFromDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return 0;
        }

        int loadedCount = 0;
        string[] dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);

        foreach (string dllFile in dllFiles)
        {
            try
            {
                var alc = new PluginAssemblyLoadContext(dllFile);
                Assembly assembly = alc.LoadFromAssemblyPath(dllFile);

                Type[] providerTypes = assembly.GetTypes()
                    .Where(t => typeof(IReaderProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToArray();

                if (providerTypes.Length > 0)
                {
                    _loadContexts.Add(alc);
                    foreach (Type type in providerTypes)
                    {
                        if (Activator.CreateInstance(type) is IReaderProvider provider)
                        {
                            if (_providers.TryAdd(provider.ProviderId, provider))
                            {
                                loadedCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore incompatible non-plugin assemblies gracefully
            }
        }

        return loadedCount;
    }

    /// <summary>
    /// Gets a provider by its unique provider ID.
    /// </summary>
    public IReaderProvider? GetProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) return null;
        _providers.TryGetValue(providerId, out var provider);
        return provider;
    }

    /// <summary>
    /// Creates a reader connection using the provider matching config.ProviderId.
    /// </summary>
    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var provider = GetProvider(config.ProviderId);
        if (provider == null)
        {
            throw new KeyNotFoundException($"No registered RFID reader provider found for ID '{config.ProviderId}'.");
        }

        return provider.CreateConnectionAsync(config, ct);
    }
}
