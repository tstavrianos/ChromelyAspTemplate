namespace ChromelyAspTemplate
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Chromely.Core;
    using Chromely.Core.Configuration;
    using Chromely.Core.Helpers;
    using Chromely.Core.Infrastructure;
    using Microsoft.AspNetCore.Hosting;

    internal static class Program
    {
        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void HideWindow()
        {
            var hWnd = Process.GetCurrentProcess().MainWindowHandle;
            ShowWindow(hWnd, 0);
        }

        static void Main(string[] args)
        {
#if DEBUG
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
#else
            if (ChromelyRuntime.Platform == ChromelyPlatform.Windows)
            {
                HideWindow();
            }
#endif

            var hostPort = GetFreeTcpPort();
            var baseAddress = IPAddress.Loopback.ToString();

            var _webApp = new WebHostBuilder()
                .UseSockets()
                .UseStartup<Startup>()
                .UseKestrel(options =>
                    {
                        options.Limits.MaxConcurrentConnections = null;
                        options.Listen(IPAddress.Loopback, hostPort);
                    })
                .Build();

            var _cancel = new CancellationTokenSource();
            _webApp.RunAsync(_cancel.Token);

            var startUrl = $"http://{baseAddress}:{hostPort}";
            var config = DefaultConfiguration.CreateForRuntimePlatform();
            config.CefDownloadOptions.DownloadSilently = true;
            config.UrlSchemes.Add(new UrlScheme(DefaultSchemeName.RESOURCE, "http", "localhost", string.Empty, UrlSchemeType.Resource, false));
            config.StartUrl = startUrl;
#if DEBUG
            config.DebuggingMode = true;
#else
            config.DebuggingMode = false;
#endif

            AppBuilder
                .Create()
                .UseConfiguration<DefaultConfiguration>(config)
                .UseApp<MyChromelyApp>()
                .Build()
                .Run(args);
        }
    }
}
