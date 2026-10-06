/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Proxy local et API locale de génération.
EN: Local proxy and local generation API.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace OpenCodeLocalAI;

public sealed class LocalProxyServer : IAsyncDisposable
{
    private readonly int _listenPort;
    private readonly int _targetPort;
    private readonly string _imagesDir;
    private readonly Action<string,string> _log;
    private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public bool Running => _listener?.IsListening == true;

    public LocalProxyServer(int listenPort, int targetPort, string imagesDir, Action<string,string> log)
    {
        _listenPort = listenPort;
        _targetPort = targetPort;
        _imagesDir = imagesDir;
        _log = log;
    }

    public Task StartAsync()
    {
        if (Running) return Task.CompletedTask;
        Directory.CreateDirectory(_imagesDir);
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{_listenPort}/");
        _listener.Start();
        _loop = Task.Run(() => LoopAsync(_cts.Token));
        _log("Proxy", $"http://127.0.0.1:{_listenPort} -> OpenCode:{_targetPort}");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        if (_loop is not null)
        {
            try { await _loop; } catch { }
        }
        _listener?.Close();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (Exception ex) when (
                IsExpectedDisconnect(ex, ct))
            {
                if (ct.IsCancellationRequested)
                    break;

                continue;
            }
            catch (Exception ex)
            {
                _log("Proxy !", ex.Message);
                await Task.Delay(100, ct);
                continue;
            }

            _ = Task.Run(() => HandleAsync(ctx, ct), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (path.Equals("/__opencodelocalai_health", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Headers["X-DreamRaster-Service"] = "proxy";
                await WriteTextAsync(ctx.Response, JsonSerializer.Serialize(new
                {
                    service = "proxy",
                    pid = Environment.ProcessId
                }), "application/json", ct);
                return;
            }

            if (ctx.Request.IsWebSocketRequest)
            {
                await ProxyWebSocketAsync(ctx, ct);
                return;
            }

            if (path.StartsWith("/local-images/", StringComparison.OrdinalIgnoreCase))
            {
                var name = Uri.UnescapeDataString(path["/local-images/".Length..]);
                name = Path.GetFileName(name);
                var file = Path.Combine(_imagesDir, name);
                if (!File.Exists(file))
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    return;
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = Mime(file);
                await using var fs = File.OpenRead(file);
                ctx.Response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(ctx.Response.OutputStream, ct);
                ctx.Response.Close();
                return;
            }

            await ProxyHttpAsync(ctx, ct);
        }
        catch (Exception ex) when (
            IsExpectedDisconnect(ex, ct))
        {
            try { ctx.Response.Close(); } catch { }
        }
        catch (Exception ex)
        {
            _log("Proxy !", ex.Message);
            try
            {
                ctx.Response.StatusCode = 502;
                ctx.Response.Close();
            }
            catch { }
        }
    }

    private static bool IsExpectedDisconnect(
        Exception ex,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested ||
            ex is OperationCanceledException ||
            ex is ObjectDisposedException)
        {
            return true;
        }

        if (ex is SocketException socket)
        {
            return socket.SocketErrorCode is
                SocketError.OperationAborted or
                SocketError.ConnectionAborted or
                SocketError.ConnectionReset or
                SocketError.NetworkReset or
                SocketError.Shutdown or
                SocketError.NotConnected or
                SocketError.NotSocket;
        }

        if (ex is HttpListenerException listener)
        {
            // 995 = operation aborted, 64 = network name unavailable,
            // 1229 = ERROR_CONNECTION_INVALID (browser/network peer disconnected).
            return listener.ErrorCode is 995 or 64 or 1229;
        }

        if (ex is Win32Exception win32 && win32.NativeErrorCode == 1229)
            return true;

        return ex.InnerException is not null &&
               IsExpectedDisconnect(ex.InnerException, ct);
    }

    private async Task ProxyHttpAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var target = new Uri($"http://127.0.0.1:{_targetPort}{ctx.Request.RawUrl}");
        using var req = new HttpRequestMessage(new HttpMethod(ctx.Request.HttpMethod), target);

        foreach (var key in ctx.Request.Headers.AllKeys)
        {
            if (key is null) continue;
            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = ctx.Request.Headers[key];
            if (value is not null) req.Headers.TryAddWithoutValidation(key, value);
        }

        if (ctx.Request.HasEntityBody)
        {
            req.Content = new StreamContent(ctx.Request.InputStream);
            if (!string.IsNullOrWhiteSpace(ctx.Request.ContentType))
                req.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
        }

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        ctx.Response.StatusCode = (int)res.StatusCode;

        foreach (var h in res.Headers)
            try { ctx.Response.Headers[h.Key] = string.Join(", ", h.Value); } catch { }
        foreach (var h in res.Content.Headers)
            try
            {
                if (!h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    ctx.Response.Headers[h.Key] = string.Join(", ", h.Value);
            }
            catch { }

        await res.Content.CopyToAsync(ctx.Response.OutputStream, ct);
        ctx.Response.Close();
    }

    private async Task ProxyWebSocketAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var target = new Uri($"ws://127.0.0.1:{_targetPort}{ctx.Request.RawUrl}");
        using var upstream = new ClientWebSocket();
        await upstream.ConnectAsync(target, ct);
        var accepted = await ctx.AcceptWebSocketAsync(null);
        using var downstream = accepted.WebSocket;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var a = RelayAsync(downstream, upstream, linked.Token);
        var b = RelayAsync(upstream, downstream, linked.Token);
        await Task.WhenAny(a, b);
        linked.Cancel();
        try { await Task.WhenAll(a, b); } catch { }
    }

    private static async Task RelayAsync(WebSocket source, WebSocket target, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        while (!ct.IsCancellationRequested &&
               source.State == WebSocketState.Open &&
               target.State == WebSocketState.Open)
        {
            var r = await source.ReceiveAsync(buffer, ct);
            if (r.MessageType == WebSocketMessageType.Close)
            {
                try { await target.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "proxy", CancellationToken.None); } catch { }
                return;
            }
            await target.SendAsync(buffer.AsMemory(0, r.Count), r.MessageType, r.EndOfMessage, ct);
        }
    }

    private static string Mime(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

    private static async Task WriteTextAsync(
        HttpListenerResponse response, string text, string contentType, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentType = contentType;
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, ct);
        response.Close();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _http.Dispose();
    }
}

public sealed class GenerationApiServer : IAsyncDisposable
{
    private readonly int _port;
    private readonly Func<string,int,int,CancellationToken,Task<ImageGenerationResult>> _generate;
    private readonly Action<string,string> _log;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public bool Running => _listener?.IsListening == true;

    public GenerationApiServer(
        int port,
        Func<string,int,int,CancellationToken,Task<ImageGenerationResult>> generate,
        Action<string,string> log)
    {
        _port = port;
        _generate = generate;
        _log = log;
    }

    public Task StartAsync()
    {
        if (Running) return Task.CompletedTask;
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _listener.Start();
        _loop = Task.Run(() => LoopAsync(_cts.Token));
        _log("API", $"Génération : http://127.0.0.1:{_port}/generate");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        if (_loop is not null)
        {
            try { await _loop; } catch { }
        }
        _listener?.Close();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch when (ct.IsCancellationRequested) { break; }
            catch { continue; }
            _ = Task.Run(() => HandleAsync(ctx, ct), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (path.Equals("/__opencodelocalai_health", StringComparison.OrdinalIgnoreCase))
            {
                await JsonAsync(ctx, new { service = "generation-api", pid = Environment.ProcessId }, 200, ct);
                return;
            }

            if (!path.Equals("/generate", StringComparison.OrdinalIgnoreCase) ||
                !ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await JsonAsync(ctx, new { error = "Not found" }, 404, ct);
                return;
            }

            using var sr = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8);
            var raw = await sr.ReadToEndAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var prompt = root.TryGetProperty("prompt", out var p) ? p.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await JsonAsync(ctx, new { ok = false, error = "prompt absent" }, 400, ct);
                return;
            }

            var width = root.TryGetProperty("width", out var w) ? w.GetInt32() : 1024;
            var height = root.TryGetProperty("height", out var h) ? h.GetInt32() : 1024;
            var result = await _generate(prompt!, width, height, ct);
            await JsonAsync(ctx, result, result.Ok ? 200 : 500, ct);
        }
        catch (Exception ex)
        {
            _log("API !", ex.Message);
            try { await JsonAsync(ctx, new { ok = false, error = ex.Message }, 500, ct); } catch { }
        }
    }

    private static async Task JsonAsync(HttpListenerContext ctx, object value, int status, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.Headers["X-DreamRaster-Service"] = "generation-api";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct);
        ctx.Response.Close();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
