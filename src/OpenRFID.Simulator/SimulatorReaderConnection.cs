using OpenRFID.Core.Abstractions;
using OpenRFID.Simulator.Profiles;

namespace OpenRFID.Simulator;

/// <summary>
/// Driver connection implementation for OpenRFID Hardware Simulator.
/// </summary>
public sealed class SimulatorReaderConnection : IReaderConnection
{
    private readonly ISimulatorProfile _profile;
    private CancellationTokenSource? _cts;
    private Task? _streamTask;

    public string ReaderId { get; private set; }
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public SimulatorReaderConnection(string readerId, ISimulatorProfile profile)
    {
        ReaderId = readerId ?? "Simulator-01";
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        var prevState = State;
        State = ConnectionState.Connected;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, prevState, ConnectionState.Connected, "Connected to virtual simulator."));

        _cts = new CancellationTokenSource();
        _streamTask = Task.Run(() => StreamLoopAsync(_cts.Token), ct);

        return Task.CompletedTask;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return;

        var prevState = State;
        State = ConnectionState.Disconnected;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_streamTask != null)
            {
                try
                {
                    await _streamTask;
                }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }

        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, prevState, ConnectionState.Disconnected, "Disconnected from virtual simulator."));
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ReaderId = config.ReaderId;
        return Task.CompletedTask;
    }

    private async Task StreamLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var tag in _profile.GenerateStreamAsync(ct))
            {
                if (State == ConnectionState.Connected)
                {
                    TagRead?.Invoke(this, new TagReadEventArgs(tag));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        catch (Exception ex)
        {
            var prevState = State;
            State = ConnectionState.Faulted;
            StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, prevState, ConnectionState.Faulted, ex.Message, ex));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
