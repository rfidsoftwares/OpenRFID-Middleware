using System.IO.Ports;
using System.Text;
using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Serial;

public sealed class SerialReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private SerialPort? _serialPort;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public SerialReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        UpdateState(ConnectionState.Connecting, "Opening Serial Port...");
        try
        {
            string portName = _config.ComPort ?? "COM1";
            int baudRate = _config.BaudRate ?? 115200;

            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            _serialPort.Open();

            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));

            UpdateState(ConnectionState.Connected, $"Serial Port {portName} opened at {baudRate} baud.");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"Serial Port connection failed: {ex.Message}", ex);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return;

        var oldState = State;
        State = ConnectionState.Disconnected;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_readTask != null)
            {
                try { await _readTask; } catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }

        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
        }
        _serialPort?.Dispose();
        _serialPort = null;

        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "Serial Port closed"));
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_serialPort == null) return;

        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested && _serialPort.IsOpen)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    string data = _serialPort.ReadExisting();
                    sb.Append(data);

                    string content = sb.ToString();
                    int newlineIndex;
                    while ((newlineIndex = content.IndexOf('\n')) >= 0)
                    {
                        string line = content[..newlineIndex].Trim();
                        content = content[(newlineIndex + 1)..];
                        sb.Clear();
                        sb.Append(content);

                        ProcessLine(line);
                    }
                }
                else
                {
                    await Task.Delay(20, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                UpdateState(ConnectionState.Faulted, $"Serial Port read error: {ex.Message}", ex);
                break;
            }
        }
    }

    public void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        try
        {
            if (line.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                string epc = root.TryGetProperty("epc", out var epcProp) ? epcProp.GetString() ?? "" : "";
                string? tid = root.TryGetProperty("tid", out var tidProp) ? tidProp.GetString() : null;
                float rssi = root.TryGetProperty("rssi", out var rssiProp) && rssiProp.TryGetSingle(out var r) ? r : -50f;
                int antenna = root.TryGetProperty("antenna", out var antProp) && antProp.TryGetInt32(out var a) ? a : 1;

                if (!string.IsNullOrEmpty(epc))
                {
                    EmitTag(epc, tid, rssi, antenna);
                }
            }
            else
            {
                EmitTag(line.Trim(), null, -50f, 1);
            }
        }
        catch
        {
            EmitTag(line.Trim(), null, -50f, 1);
        }
    }

    private void EmitTag(string epc, string? tid, float rssi, int antenna)
    {
        var tag = new TagReadEvent
        {
            EPC = epc,
            TID = tid,
            RSSI = rssi,
            AntennaPort = antenna,
            ReaderId = ReaderId,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow
        };

        TagRead?.Invoke(this, new TagReadEventArgs(tag));
    }

    private void UpdateState(ConnectionState newState, string message, Exception? ex = null)
    {
        var oldState = State;
        State = newState;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, newState, message, ex));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
        await DisconnectAsync();
    }
}
