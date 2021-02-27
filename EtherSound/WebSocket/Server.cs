using EtherSound.Settings;
using EtherSound.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WASCap;

namespace EtherSound.WebSocket
{
    class Server : IDisposable
    {
        static readonly Regex UriPattern = new Regex(@"^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?", RegexOptions.Singleline | RegexOptions.Compiled);

        const string Http404Page = @"<!DOCTYPE HTML PUBLIC "" -//W3C//DTD HTML 4.01//EN""""http://www.w3.org/TR/html4/strict.dtd"">
<HTML><HEAD><TITLE>Not Found</TITLE>
<META HTTP-EQUIV=""Content-Type"" Content=""text/html; charset=us-ascii""></HEAD>
<BODY><h2>Not Found</h2>
<hr><p>HTTP Error 404. The requested resource is not found.</p>
</BODY></HTML>";

        readonly RootModel model;
        readonly SessionManager sessions;
        readonly ISet<ClientHandler> clients;
        HttpListener listener;
        CancellationTokenSource cancel;
        string prefix;
        string prefixPath;
        string relativeUri;
        string preSharedSecret;
        WebSocketPermissions globalPerms;
        WebSocketPermissions networkPerms;
        WebSocketPermissions unauthPerms;

        public string PreSharedSecret => preSharedSecret;
        public WebSocketPermissions GlobalPermissions => globalPerms;
        public WebSocketPermissions NetworkPermissions => networkPerms;
        public WebSocketPermissions UnauthenticatedPermissions => unauthPerms;

        public WebSocketSettings Settings
        {
            set
            {
                (string prefix, string prefixPath, string relativeUri) = SplitUri(value.Uri);
                if (prefix != this.prefix)
                {
                    this.prefix = prefix;
                    if (prefix == null)
                    {
                        Stop();
                    }
                    else
                    {
                        Start();
                    }
                }
                this.prefixPath = prefixPath;
                this.relativeUri = relativeUri;
                string preSharedSecret = string.IsNullOrWhiteSpace(value.PreSharedSecret) ? null : value.PreSharedSecret.Trim();
                bool pssChanged = preSharedSecret != this.preSharedSecret;
                if (pssChanged)
                {
                    this.preSharedSecret = preSharedSecret;
                }
                bool permsChanged = globalPerms != value.GlobalPermissions
                                 || networkPerms != value.NetworkPermissions
                                 || unauthPerms != value.UnauthenticatedPermissions;
                if (permsChanged)
                {
                    globalPerms = value.GlobalPermissions;
                    networkPerms = value.NetworkPermissions;
                    unauthPerms = value.UnauthenticatedPermissions;
                }
                if (pssChanged || permsChanged)
                {
                    ForEachClient(client => client.OnSecurityChanged(pssChanged, permsChanged));
                }
            }
        }

        public Server(RootModel model, SessionManager sessions, WebSocketSettings settings)
        {
            this.model = model;
            this.sessions = sessions;
            clients = new HashSet<ClientHandler>();
            (prefix, prefixPath, relativeUri) = SplitUri(settings.Uri);
            preSharedSecret = string.IsNullOrWhiteSpace(settings.PreSharedSecret) ? null : settings.PreSharedSecret.Trim();
            globalPerms = settings.GlobalPermissions;
            networkPerms = settings.NetworkPermissions;
            unauthPerms = settings.UnauthenticatedPermissions;
            model.PropertyChanged += Model_PropertyChanged;
            model.SessionPropertyChanged += Model_SessionPropertyChanged;
            model.ChannelPropertyChanged += Model_ChannelPropertyChanged;
            if (null != prefix)
            {
                Start();
            }
        }

        void Start()
        {
            if (null == prefix)
            {
                throw new InvalidOperationException();
            }

            if (null == cancel)
            {
                MainLoop();
            }
            else
            {
                cancel.Cancel();
                ((IDisposable)listener).Dispose();
                MainLoop();
            }
        }

        async Task StartListener()
        {
            cancel = new CancellationTokenSource();
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
                return;
            }
            catch (HttpListenerException e)
            {
                if (e.NativeErrorCode != 5)
                {
                    throw;
                }
            }

            Program.SuspendSavingSettings();
            try
            {
                await Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "http add urlacl " + string.Format("url={0}", prefix).EncodeParameterArgument() + " " + string.Format("user={0}\\{1}", Environment.UserDomainName, Environment.UserName).EncodeParameterArgument(),
                    Verb = "runas",
                    UseShellExecute = true,
                }).WaitForExitAsync(cancel.Token);
            }
            finally
            {
                Program.ResumeSavingSettings();
            }

            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            listener.Start();
        }

        void Stop()
        {
            if (null == cancel)
            {
                return;
            }

            cancel.Cancel();
            ForEachClient(client => client.Dispose());
            ((IDisposable)listener).Dispose();
            listener = null;
            cancel = null;
        }

        void ForEachClient(Action<ClientHandler> action)
        {
            List<ClientHandler> clients;
            lock (this.clients)
            {
                clients = new List<ClientHandler>(this.clients);
            }
            foreach (ClientHandler client in clients)
            {
                action(client);
            }
        }

        void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ForEachClient(client => client.OnRootPropertyChanged(e));
        }

        void Model_SessionPropertyChanged(object sender, SessionPropertyChangedEventArgs e)
        {
            ForEachClient(client => client.OnSessionPropertyChanged(e));
        }

        void Model_ChannelPropertyChanged(object sender, ChannelPropertyChangedEventArgs e)
        {
            ForEachClient(client => client.OnChannelPropertyChanged(e));
        }

        async void MainLoop()
        {
            await StartListener();
            try
            {
                for (; ; )
                {
                    HttpListenerContext ctx = await listener.GetContextAsync().WaitOrCancel(cancel.Token);
                    string path = ctx.Request.Url.AbsolutePath;
                    if (!path.StartsWith(prefixPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Serve404(ctx);
                        continue;
                    }
                    path = path.Substring(prefixPath.Length);
                    bool isWebSocketPath = path.Equals(relativeUri, StringComparison.InvariantCultureIgnoreCase);
                    if (isWebSocketPath && ctx.Request.IsWebSocketRequest)
                    {
                        await Accept(ctx);
                        continue;
                    }

                    string localPath = Path.Combine(Path.GetDirectoryName(typeof(Server).Assembly.Location), "EtherSound.DocumentRoot", path);
                    if (Directory.Exists(localPath))
                    {
                        if (!ServeFile(ctx, Path.Combine(localPath, "index.html"))
                         && !ServeFile(ctx, Path.Combine(localPath, "index.htm"))
                         && !ServeFile(ctx, Path.Combine(localPath, "index.txt")))
                        {
                            if (isWebSocketPath)
                            {
                                Serve426(ctx);
                            }
                            else
                            {
                                Serve404(ctx);
                            }
                        }
                    }
                    else if (!ServeFile(ctx, localPath))
                    {
                        if (isWebSocketPath)
                        {
                            Serve426(ctx);
                        }
                        else
                        {
                            Serve404(ctx);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        async Task Accept(HttpListenerContext ctx)
        {
            HttpListenerWebSocketContext wsCtx = await ctx.AcceptWebSocketAsync("ethersound").WaitOrCancel(cancel.Token);
            Program.Queue(() =>
            {
                ClientHandler client = null;
                client = new ClientHandler(wsCtx, model, sessions, this, () =>
                {
                    lock (clients)
                    {
                        clients.Remove(client);
                    }
                });
                lock (clients)
                {
                    clients.Add(client);
                }
            });
        }

        static bool ServeFile(HttpListenerContext ctx, string localPath)
        {
            if (!File.Exists(localPath))
            {
                return false;
            }

            ctx.Response.StatusCode = 200;
            byte[] contents = File.ReadAllBytes(localPath);
            string contentType = GetContentType(Path.GetExtension(localPath));
            if (contentType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase))
            {
                using (MemoryStream cStream = new MemoryStream(contents))
                {
                    using (StreamReader detector = new StreamReader(cStream, Encoding.UTF8, true))
                    {
                        detector.Peek();
                        contentType += "; charset=" + detector.CurrentEncoding.HeaderName;
                    }
                }
            }
            ctx.Response.AddHeader("Content-Type", contentType);
            ctx.Response.Close(contents, false);

            return true;
        }

        static void Serve404(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.AppendHeader("Content-Type", "text/html; charset=us-ascii");
            ctx.Response.Close(Encoding.ASCII.GetBytes(Http404Page), false);
        }

        static void Serve426(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 426;
            ctx.Response.AppendHeader("Content-Type", "text/plain; charset=us-ascii");
            ctx.Response.AppendHeader("Upgrade", "websocket");
            ctx.Response.Close(Encoding.ASCII.GetBytes("Hello, I'm actually a WebSocket. [HTTP 426 Upgrade Required]"), false);
        }
        
        static string GetContentType(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".htm":
                case ".html":
                    return "text/html";
                case ".txt":
                    return "text/plain";
                case ".css":
                    return "text/css";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".ico":
                    return "image/x-icon";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".webp":
                    return "image/webp";
                case ".svg":
                    return "image/svg+xml";
                case ".js":
                    return "application/javascript";
                case ".json":
                    return "application/json";
                default:
                    return "application/octet-stream";
            }
        }

        static (string prefix, string prefixPath, string relativeUri) SplitUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return (null, null, null);
            }

            Match match = UriPattern.Match(uri.Trim());
            string scheme = match.Groups[2].Success ? match.Groups[2].Value : "http";
            string host = match.Groups[4].Success ? match.Groups[4].Value : null;
            string path = match.Groups[5].Value;
            if (scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase) || scheme.Equals("wss", StringComparison.InvariantCultureIgnoreCase))
            {
                scheme = "https";
            }
            else
            {
                scheme = "http";
            }
            if (null == host)
            {
                host = "localhost";
            }

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            int splitPoint = path.LastIndexOf('/') + 1;
            string prefixPath = path.Substring(0, splitPoint);

            return (string.Format("{0}://{1}{2}", scheme, host, prefixPath), prefixPath, path.Substring(splitPoint));
        }

        public void Dispose()
        {
            Stop();
            model.ChannelPropertyChanged -= Model_ChannelPropertyChanged;
            model.SessionPropertyChanged -= Model_SessionPropertyChanged;
            model.PropertyChanged -= Model_PropertyChanged;
        }
    }
}
