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

public sealed class IpcServer : IDisposable
{
    public event EventHandler<bool> ConnectedChanged; // true = connected

    private const string PipeName = "LiT.LiTRE.VSCodePipe";
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task _acceptLoop;
    private int _connected; // 0/1

    public void Start()
    {
        _acceptLoop = AcceptLoopAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _acceptLoop?.Wait(2000); } catch { /* ignore */ }
    }

    private static PipeSecurity CurrentUserOnly()
    {
        var ps = new PipeSecurity();
        var user = WindowsIdentity.GetCurrent().User;
        ps.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
        return ps;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using (var server = new NamedPipeServerStream(
                PipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                4096, 4096, CurrentUserOnly()))
            {
                try
                {
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                    SetConnected(true);

                    var reader = new StreamReader(server, new UTF8Encoding(false), false, 4096, true);
                    var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true };

                    // Optional hello
                    await writer.WriteLineAsync("{\"v\":1,\"type\":\"event\",\"evt\":\"hello\",\"payload\":{\"from\":\"winforms\"}}").ConfigureAwait(false);

                    while (!ct.IsCancellationRequested && server.IsConnected)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var obj = JObject.Parse(line);
                            var type = (string)obj["type"];
                            if (type == "req")
                            {
                                var reqId = (string)obj["requestId"] ?? Guid.NewGuid().ToString("N");
                                var cmd = (string)obj["cmd"];
                                var payload = obj["payload"] as JObject;

                                switch (cmd)
                                {
                                    case "saveScript":
                                        int id = payload != null && payload["id"] != null ? (int)payload["id"] : -1;
                                        string path = payload != null ? (string)payload["path"] : null;
                                        bool ok = SaveScriptInYourApp(id, path);
                                        await ReplyAsync(writer, reqId, ok,
                                            ok ? new { saved = true, id, path } : null,
                                            ok ? null : new { code = "SAVE_FAILED", message = "Could not save." }
                                        ).ConfigureAwait(false);
                                        break;

                                    default:
                                        await ReplyAsync(writer, reqId, false, null,
                                            new { code = "UNKNOWN_CMD", message = "Unknown cmd '" + cmd + "'." }
                                        ).ConfigureAwait(false);
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Bad message: " + ex);
                        }
                    }
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Pipe error: " + ex);
                    await Task.Delay(800, ct).ConfigureAwait(false);
                }
                finally
                {
                    SetConnected(false);
                }
            }
        }
    }

    private static Task ReplyAsync(StreamWriter w, string reqId, bool ok, object result, object error)
    {
        var payload = new
        {
            v = 1,
            type = "res",
            requestId = reqId,
            ok = ok,
            result = result,
            error = error
        };
        var json = JsonConvert.SerializeObject(payload);
        return w.WriteLineAsync(json);
    }

    private bool SaveScriptInYourApp(int id, string path)
    {
        // TODO: your save logic
        return true;
    }

    private void SetConnected(bool value)
    {
        var newVal = value ? 1 : 0;
        if (Interlocked.Exchange(ref _connected, newVal) != newVal)
        {
            var handler = ConnectedChanged;
            if (handler != null) handler(this, value);
        }
    }
}
