using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Orchestration;

namespace OpenRFID.Management.Api.WebSockets;

public sealed class TagWebSocketHandler
{
    private readonly MiddlewareOrchestrator _orchestrator;
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TagWebSocketHandler(MiddlewareOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _orchestrator.RawTagReceived += OnRawTagReceived;
        _orchestrator.FilteredTagDispatched += OnFilteredTagDispatched;
    }

    public async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket)
    {
        var id = Guid.NewGuid();
        _sockets.TryAdd(id, webSocket);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Connection closed or aborted
        }
        finally
        {
            _sockets.TryRemove(id, out _);
        }
    }

    private void OnRawTagReceived(object? sender, TagReadEvent tag)
    {
        BroadcastMessage("raw", tag);
    }

    private void OnFilteredTagDispatched(object? sender, TagReadEvent tag)
    {
        BroadcastMessage("filtered", tag);
    }

    private void BroadcastMessage(string type, TagReadEvent tag)
    {
        if (_sockets.IsEmpty) return;

        var msg = JsonSerializer.Serialize(new
        {
            type,
            tag
        }, _jsonOptions);

        var bytes = Encoding.UTF8.GetBytes(msg);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, socket) in _sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                _ = SendSafeAsync(id, socket, segment);
            }
            else
            {
                _sockets.TryRemove(id, out _);
            }
        }
    }

    private async Task SendSafeAsync(Guid id, WebSocket socket, ArraySegment<byte> segment)
    {
        try
        {
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            _sockets.TryRemove(id, out _);
        }
    }
}
