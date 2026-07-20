using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenRFID.Simulator.Profiles;

namespace OpenRFID.Simulator.Servers;

/// <summary>
/// Async TCP Socket server broadcasting simulated tag streams over network sockets.
/// </summary>
public sealed class TcpSocketSimulatorServer : IAsyncDisposable
{
    private readonly int _port;
    private readonly ISimulatorProfile _profile;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly List<TcpClient> _clients = new();
    private readonly object _lock = new();

    public bool IsRunning { get; private set; }

    public TcpSocketSimulatorServer(int port = 5084, ISimulatorProfile? profile = null)
    {
        _port = port;
        _profile = profile ?? new StaticInventoryProfile();
    }

    public void Start()
    {
        if (IsRunning) return;

        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        IsRunning = true;
        _cts = new CancellationTokenSource();

        _listenTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
        _ = Task.Run(() => BroadcastLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                lock (_lock)
                {
                    _clients.Add(client);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { }
        }
    }

    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var tag in _profile.GenerateStreamAsync(ct))
            {
                var json = JsonSerializer.Serialize(tag) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);

                List<TcpClient> activeClients;
                lock (_lock)
                {
                    activeClients = _clients.ToList();
                }

                foreach (var client in activeClients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            var stream = client.GetStream();
                            await stream.WriteAsync(bytes, ct);
                        }
                        else
                        {
                            lock (_lock) { _clients.Remove(client); }
                            client.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                        lock (_lock) { _clients.Remove(client); }
                        client.Dispose();
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        IsRunning = false;

        _cts?.Cancel();
        _listener?.Stop();

        lock (_lock)
        {
            foreach (var client in _clients)
            {
                client.Dispose();
            }
            _clients.Clear();
        }

        if (_listenTask != null)
        {
            try { await _listenTask; } catch (OperationCanceledException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}
