using System.Reflection;
using System.Runtime.Loader;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Engine.Plugins;

/// <summary>
/// Isolated AssemblyLoadContext for loading driver plugin assemblies dynamically while sharing Core Abstractions with the host.
/// </summary>
public sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
    {
        ArgumentNullException.ThrowIfNull(pluginPath);
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Force sharing of Core Abstractions with the host context to avoid type mismatch exceptions
        if (string.Equals(assemblyName.Name, typeof(IReaderProvider).Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
        {
            return null; // Fallback to default load context
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}
