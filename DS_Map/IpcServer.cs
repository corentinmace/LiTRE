using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSPRE.ROMFiles;
using LiTRE.ROMFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiTRE
{
    public sealed class IpcResponse
    {
        public bool Ok { get; set; }
        public string Error { get; set; }

        // Arbitrary payload
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public JToken Data { get; set; }

        public static IpcResponse Success() => new IpcResponse { Ok = true };

        public static IpcResponse Success<T>(T data) => new IpcResponse
        {
            Ok = true,
            Data = data != null ? JToken.FromObject(data) : JValue.CreateNull()
        };

        public static IpcResponse Fail(string message) => new IpcResponse
        {
            Ok = false,
            Error = message
        };

        // Convenience for clients (if reused there)
        public T As<T>()
        {
            if (Data == null || Data.Type == JTokenType.Null) return default(T);
            return Data.ToObject<T>();
        }
    }

    /// <summary>
    /// Named-pipe server that marshals UI work via SynchronizationContext.
    /// Supply a UI handler delegate in the constructor.
    /// </summary>
    public sealed class IpcServer : IDisposable
    {
        public const string PipeName = "LiT.LiTRE.VSCodePipe";

        // Events
        public event EventHandler<bool> ConnectedChanged;

        // UI marshaling
        private readonly SynchronizationContext _ui;

        // Handlers you provide (executed on UI thread)
        private readonly Func<int, string, IpcResponse> _saveScriptHandler;
        private readonly Func<int, string, IpcResponse> _openRelatedHandler;
        private readonly Func<int, IpcEvents.EventFileAndImages> _getEventData;
        private readonly Func<int, TextArchive> _getArchive;
        private readonly Func<int, IpcEvents.HeaderData> _getHeaderData;
        private readonly Func<IpcResponse> _saveRom;

        // Optional logger
        private readonly Action<string> _log;

        // Lifecycle
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _acceptLoop;

        // Connection state
        private readonly object _sendLock = new object();
        private NamedPipeServerStream _server;   // current client
        private StreamWriter _writer;            // UTF-8 writer for outbound messages

        public bool IsConnected
        {
            get
            {
                lock (_sendLock) { return _server?.IsConnected == true; }
            }
        }

        public IpcServer(
            SynchronizationContext uiContext,
            Func<int, string, IpcResponse> saveScriptHandler,
            Func<int, string, IpcResponse> openRelatedHandler,
            Func<int, IpcEvents.EventFileAndImages> getEventData,
            Func<int, TextArchive> getArchive,
            Func<int, IpcEvents.HeaderData> getHeaderData,
            Func<IpcResponse> saveRom,
            Action<string> logger = null)
        {
            if (uiContext == null)
                throw new ArgumentNullException(nameof(uiContext), "Pass SynchronizationContext.Current from your UI thread.");
            if (saveScriptHandler == null)
                throw new ArgumentNullException(nameof(saveScriptHandler));

            _ui = uiContext;
            _saveScriptHandler = saveScriptHandler;
            _openRelatedHandler = openRelatedHandler;
            _getEventData = getEventData;
            _getArchive = getArchive;
            _getHeaderData = getHeaderData;
            _saveRom = saveRom;
            _log = logger;
        }

        public void Start()
        {
            if (_acceptLoop != null) return;
            _acceptLoop = AcceptLoopAsync(_cts.Token);
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _acceptLoop?.Wait(2000); } catch { }
            CleanupConnection();
            _cts.Dispose();
        }

        // ---- Public outbound API -------------------------------------------------

        // Fire-and-forget event
        public void PushEvent(string name, object payload = null)
        {
            var env = new { v = 1, type = "event", evt = name, payload };
            try { SendEnvelope(env); } catch { /* swallow by design for fire-and-forget */ }
        }

        // Awaitable event (propagates "not connected" etc.)
        public Task PushEventAsync(string name, object payload = null)
        {
            var env = new { v = 1, type = "event", evt = name, payload };
            return SendEnvelopeAsync(env);
        }

        // ---- Accept + per-connection loop ---------------------------------------

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeServerStream server = null;
                StreamWriter writer = null;

                try
                {
                    server = CreateServerPipe();
                    Log("IPC: waiting for connection…");
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                    // Install as current connection
                    lock (_sendLock)
                    {
                        _server = server;
                        _writer = writer = new StreamWriter(server, new UTF8Encoding(false), 1024, leaveOpen: true)
                        {
                            AutoFlush = true
                        };
                    }
                    RaiseConnectedChanged(true);

                    // Optional hello
                    await SendEnvelopeAsync(writer, new
                    {
                        v = 1,
                        type = "event",
                        evt = "hello",
                        payload = new { msg = "hello from app" }
                    }).ConfigureAwait(false);

                    // Read loop for this client
                    using (var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
                    {
                        while (!ct.IsCancellationRequested && server.IsConnected)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (line == null)
                            {
                                Log("IPC: client disconnected (EOF).");
                                break;
                            }
                            if (line.Length == 0) continue;

                            try
                            {
                                await HandleLineAsync(line, writer).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                Log("IPC: handle error: " + ex.Message);
                                // keep processing further lines
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    Log("IPC: server error: " + ex.Message);
                }
                finally
                {
                    // Clear current connection and notify
                    CleanupConnection();
                    RaiseConnectedChanged(false);

                    // Local disposals if not already disposed in CleanupConnection
                    if (server != null)
                        try { server.Dispose(); } catch { }
                    if (writer != null)
                        try { writer.Dispose(); } catch { }
                }

                if (!ct.IsCancellationRequested)
                    try { await Task.Delay(300, ct).ConfigureAwait(false); } catch (OperationCanceledException) { }
            }
        }

        private static NamedPipeServerStream CreateServerPipe()
        {
            // Restrict to current user
            var sec = new PipeSecurity();
            var user = WindowsIdentity.GetCurrent().User;
            sec.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));

            return new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                1, // single client
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                inBufferSize: 0,
                outBufferSize: 0,
                pipeSecurity: sec);
        }

        private async Task HandleLineAsync(string line, StreamWriter writer)
        {
            string requestIdForError = null;

            try
            {
                var obj = JObject.Parse(line);
                var type = (string)obj["type"];
                if (!string.Equals(type, "req", StringComparison.OrdinalIgnoreCase))
                    return; // ignore non-requests (extend as needed)

                var requestId = (string)obj["requestId"];
                requestIdForError = requestId;
                var cmd = (string)obj["cmd"];
                var payload = obj["payload"] as JObject ?? new JObject();

                if (string.Equals(cmd, "ping", StringComparison.OrdinalIgnoreCase))
                {
                    await SendResponseAsync(writer, requestId, IpcResponse.Success()).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(cmd, "saveScript", StringComparison.OrdinalIgnoreCase))
                {
                    int id = payload["id"] != null ? (int)payload["id"] : 0;
                    string path = payload["path"] != null ? (string)payload["path"] : null;

                    var res = await InvokeOnUiAsync(() =>
                    {
                        return _saveScriptHandler(id, path);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, res).ConfigureAwait(false);
                    return;
                }
                
                if (string.Equals(cmd, "openRelated", StringComparison.OrdinalIgnoreCase))
                {
                    int id = payload["id"] != null ? (int)payload["id"] : 0;
                    string related = payload["related"] != null ? (string)payload["related"] : null;

                    var res = await InvokeOnUiAsync(() =>
                    {
                        return _openRelatedHandler(id, related);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, res).ConfigureAwait(false);
                    return;
                }
                
                if (string.Equals(cmd, "getEventData", StringComparison.OrdinalIgnoreCase))
                {
                    int id = payload["id"] != null ? (int)payload["id"] : 0;

                    // Marshal to UI, get the object (EventFile), wrap into IpcResponse with payload
                    var data = await InvokeOnUiAsync(() =>
                    {
                        return _getEventData(id);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, IpcResponse.Success(data)).ConfigureAwait(false);
                    return;
                } 
                
                if (string.Equals(cmd, "getMessageData", StringComparison.OrdinalIgnoreCase))
                {
                    int id = payload["id"] != null ? (int)payload["id"] : 0;

                    // Marshal to UI, get the object (EventFile), wrap into IpcResponse with payload
                    var data = await InvokeOnUiAsync(() =>
                    {
                        return _getArchive(id);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, IpcResponse.Success(data)).ConfigureAwait(false);
                    return;
                }
                
                if (string.Equals(cmd, "getHeaderData", StringComparison.OrdinalIgnoreCase))
                {
                    int id = payload["id"] != null ? (int)payload["id"] : 0;
                    
                    var data = await InvokeOnUiAsync(() =>
                    {
                        return _getHeaderData(id);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, IpcResponse.Success(data)).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(cmd, "saveRom", StringComparison.OrdinalIgnoreCase))
                {
                    var data = await InvokeOnUiAsync(() =>
                    {
                        return _saveRom();
                    }).ConfigureAwait(true);
                    
                    await SendResponseAsync(writer, requestId, IpcResponse.Success(data)).ConfigureAwait(false);
                    return;
                }

                await SendResponseAsync(writer, requestId, IpcResponse.Fail("Unknown cmd: " + cmd)).ConfigureAwait(false);
            }
            catch (JsonReaderException jex)
            {
                Log("IPC: bad JSON: " + jex.Message);
                if (requestIdForError != null)
                    await SendResponseAsync(writer, requestIdForError, IpcResponse.Fail("Bad JSON")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log("IPC: HandleLine failed: " + ex.Message);
                if (requestIdForError != null)
                    await SendResponseAsync(writer, requestIdForError, IpcResponse.Fail(ex.Message)).ConfigureAwait(false);
            }
        }

        private Task<IpcResponse> InvokeOnUiAsync(Func<IpcResponse> action)
        {
            var tcs = new TaskCompletionSource<IpcResponse>();
            _ui.Post(_ =>
            {
                try
                {
                    var res = action();
                    if (res == null) res = IpcResponse.Success();
                    tcs.TrySetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(IpcResponse.Fail(ex.Message));
                }
            }, null);
            return tcs.Task;
        }

        // Generic UI marshal for object-returning handlers
        private Task<T> InvokeOnUiAsync<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            _ui.Post(_ =>
            {
                try
                {
                    var res = action();
                    tcs.TrySetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        private Task SendResponseAsync(StreamWriter writer, string requestId, IpcResponse res)
        {
            var envelope = new
            {
                v = 1,
                type = "res",
                requestId = requestId,
                ok = res != null && res.Ok,
                error = (res != null && !res.Ok && !string.IsNullOrEmpty(res.Error))
                        ? new { message = res.Error }
                        : null,
                payload = res != null ? res.Data : null
            };
            return SendEnvelopeAsync(writer, envelope);
        }

        // ---- Outbound sending helpers -------------------------------------------

        // Uses provided writer (e.g., inside connection read loop)
        private static Task SendEnvelopeAsync(StreamWriter writer, object envelope)
        {
            var json = JsonConvert.SerializeObject(
                envelope,
                Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    NullValueHandling = NullValueHandling.Ignore
                });
            return writer.WriteAsync(json + "\n");
        }

        // Uses current connection's writer (thread-safe, callable from anywhere)
        private Task SendEnvelopeAsync(object envelope)
        {
            StreamWriter w;
            lock (_sendLock)
            {
                w = _writer;
                if (w == null || _server?.IsConnected != true)
                    return Task.FromException(new InvalidOperationException("IPC not connected"));
            }

            var json = JsonConvert.SerializeObject(
                envelope,
                Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    NullValueHandling = NullValueHandling.Ignore
                });

            // Write outside the lock to avoid blocking other callers
            return w.WriteAsync(json + "\n");
        }

        private void SendEnvelope(object envelope)
        {
            StreamWriter w;
            lock (_sendLock)
            {
                w = _writer;
                if (w == null || _server?.IsConnected != true) return;
            }

            var json = JsonConvert.SerializeObject(
                envelope,
                Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    NullValueHandling = NullValueHandling.Ignore
                });

            try { w.WriteLine(json); } catch { /* fire-and-forget */ }
        }

        // ---- Cleanup / notifications -------------------------------------------

        private void CleanupConnection()
        {
            lock (_sendLock)
            {
                try { _writer?.Dispose(); } catch { }
                try { _server?.Dispose(); } catch { }
                _writer = null;
                _server = null;
            }
        }

        private void RaiseConnectedChanged(bool connected)
        {
            try { ConnectedChanged?.Invoke(this, connected); } catch { }
        }

        private void Log(string msg)
        {
            try
            {
                if (_log != null) _log(msg);
                else System.Diagnostics.Debug.WriteLine(msg);
            }
            catch { }
        }
    }
}
