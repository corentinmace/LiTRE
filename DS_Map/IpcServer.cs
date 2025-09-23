using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiTRE
{
    public sealed class IpcResponse
    {
        public bool Ok { get; set; }
        public string Error { get; set; }

        public static IpcResponse Success() => new IpcResponse { Ok = true };
        public static IpcResponse Fail(string message) => new IpcResponse { Ok = false, Error = message };
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

        // Lifecycle
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _acceptLoop;

        // UI marshaling
        private readonly SynchronizationContext _ui;

        // Handlers you provide (executed on UI thread)
        private readonly Func<int, string, IpcResponse> _saveScriptHandler;

        // Optional logger
        private readonly Action<string> _log;

        public IpcServer(
            SynchronizationContext uiContext,
            Func<int, string, IpcResponse> saveScriptHandler,
            Action<string> logger = null)
        {
            if (uiContext == null)
                throw new ArgumentNullException(nameof(uiContext), "Pass SynchronizationContext.Current from your UI thread.");
            if (saveScriptHandler == null)
                throw new ArgumentNullException(nameof(saveScriptHandler));

            _ui = uiContext;
            _saveScriptHandler = saveScriptHandler;
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
            try { if (_acceptLoop != null) _acceptLoop.Wait(2000); } catch { }
            _cts.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = CreateServerPipe();
                    Log("IPC: waiting for connection…");
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                    RaiseConnectedChanged(true);

                    using (server)
                    using (var reader = new StreamReader(server, Encoding.UTF8, true, 4096, false))
                    using (var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true })
                    {
                        // Optional hello
                        await SendEnvelopeAsync(writer, new
                        {
                            v = 1,
                            type = "event",
                            evt = "hello",
                            payload = new { msg = "hello from app" }
                        }).ConfigureAwait(false);

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
                                // We continue reading; specific request errors are replied in HandleLineAsync.
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
                    RaiseConnectedChanged(false);
                    if (server != null) { try { server.Dispose(); } catch { } }
                }

                if (!ct.IsCancellationRequested)
                    await Task.Delay(300, ct).ConfigureAwait(false);
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
                1, // single client (tune if you need multiple VS Code windows)
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0, 0,
                sec);
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
                        // This runs on the UI thread
                        return _saveScriptHandler(id, path);
                    }).ConfigureAwait(false);

                    await SendResponseAsync(writer, requestId, res).ConfigureAwait(false);
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
                        : null
            };
            return SendEnvelopeAsync(writer, envelope);
        }

        private static Task SendEnvelopeAsync(StreamWriter writer, object envelope)
        {
            var json = JsonConvert.SerializeObject(envelope, Formatting.None);
            return writer.WriteAsync(json + "\n");
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
