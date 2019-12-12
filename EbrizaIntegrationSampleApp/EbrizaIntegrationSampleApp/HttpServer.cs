using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace EbrizaIntegrationSampleApp
{
    public class HttpServer
    {
        const string baseUrl = "http://localhost:9999";

        public event EventHandler<PublicUrlAcquiredEventArgs> OnPublicUrlAcquired;

        readonly Thread httpServerThread;
        readonly NancyHost nancyHost;
        public HttpServer()
        {
            nancyHost = new NancyHost(new Uri(baseUrl));

            httpServerThread = new Thread(state =>
            {
                Process ngrokProcess = null;

                try
                {
                    nancyHost.Start();
                    Console.WriteLine($"Running self hosted HTTP Server on {baseUrl}");

                    ngrokProcess = new Process();
                    ngrokProcess.StartInfo.FileName = "ngrok.exe";
                    ngrokProcess.StartInfo.Arguments = "http 9999";

                    ngrokProcess.Start();

                    using (HttpClient http = new HttpClient())
                    using (HttpResponseMessage response = http.GetAsync("http://localhost:4040/api/tunnels").Result)
                    {
                        NgrokTunnels tunnels = Newtonsoft.Json.JsonConvert.DeserializeObject<NgrokTunnels>(response.Content.ReadAsStringAsync().Result);

                        string publicUrl = tunnels.tunnels.First(t => t.proto.Equals("http", StringComparison.InvariantCultureIgnoreCase)).public_url;

                        OnPublicUrlAcquired?.Invoke(null, new PublicUrlAcquiredEventArgs(publicUrl));
                    }

                    while (true) { Thread.Sleep(1000 * 60 * 60); } //Keep thread alive
                }
                catch (ThreadAbortException)
                {
                    ngrokProcess.Kill();
                    nancyHost.Stop();
                    Console.WriteLine($"Stopped self hosted HTTP Server on {baseUrl}");
                }
            });
            httpServerThread.IsBackground = true;
        }

        public void Start()
        {
            httpServerThread.Start();
        }

        public void Stop()
        {
            httpServerThread.Abort();
        }
    }

    public class NgrokTunnels
    {
        public Tunnel[] tunnels { get; set; }

        public class Tunnel
        {
            public string public_url { get; set; }
            public string proto { get; set; }
        }
    }

    public class PublicUrlAcquiredEventArgs : EventArgs
    {
        public readonly string PublicUrl;

        public PublicUrlAcquiredEventArgs(string publicUrl)
        {
            PublicUrl = publicUrl;
        }
    }

    public class HttpEndpoints : NancyModule
    {
        public HttpEndpoints() : base()
        {
            Get("/ping", _ => $"pong @ {DateTime.Now}");
        }
    }
}
