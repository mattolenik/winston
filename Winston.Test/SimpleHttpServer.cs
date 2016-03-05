using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Linq;
using System.Threading;

namespace Winston.Test
{
    class SimpleHttpServer : IDisposable
    {
        readonly string[] indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        static readonly IDictionary<string, string> MimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list

                {".exe", "application/octet-stream"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".msi", "application/octet-stream"},
                {".rar", "application/x-rar-compressed"},
                {".zip", "application/zip"},

                #endregion
            };

        Thread serverThread;
        string rootDirectory;
        string indexFile;
        HttpListener listener;
        bool running;

        public int Port { get; private set; }

        /// <summary>
        /// Creates a new server serving the specified path and listening on the specified port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHttpServer(string path, int port)
        {
            Initialize(path, port);
        }

        /// <summary>
        /// Creates a new server serving the specified path.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHttpServer(string path)
        {
            //get an empty port
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            Initialize(path, port);
        }

        /// <summary>
        /// Stop server
        /// </summary>
        public void Stop()
        {
            running = false;
        }

        void Listen()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{Port}/");
            listener.Start();
            running = true;
            while (running)
            {
                try
                {
                    var context = listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
            listener.Stop();
        }

        void Process(HttpListenerContext context)
        {
            using (context.Response.OutputStream)
            {
                var filename = context.Request.Url.AbsolutePath;
                Console.WriteLine(filename);
                filename = filename.Substring(1);

                if (string.IsNullOrEmpty(filename))
                {
                    filename = indexFile;
                }

                filename = Path.Combine(rootDirectory, filename);

                if (!File.Exists(filename))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                try
                {
                    using (var input = new FileStream(filename, FileMode.Open))
                    {
                        string mime;
                        context.Response.ContentType = MimeTypeMappings.TryGetValue(Path.GetExtension(filename),
                            out mime)
                            ? mime
                            : "application/octet-stream";
                        context.Response.ContentLength64 = input.Length;
                        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                        context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
                        input.CopyTo(context.Response.OutputStream);
                        context.Response.OutputStream.Flush();
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    var trace = Encoding.UTF8.GetBytes(ex.StackTrace);
                    context.Response.OutputStream.Write(trace, 0, trace.Length);
                    context.Response.ContentLength64 = trace.Length;
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentType = "text/plain";
                }
            }
        }

        void Initialize(string path, int port)
        {
            rootDirectory = path;
            indexFile = indexFiles.FirstOrDefault(f => File.Exists(Path.Combine(rootDirectory, f)));
            Port = port;
            serverThread = new Thread(Listen);
            serverThread.Start();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}