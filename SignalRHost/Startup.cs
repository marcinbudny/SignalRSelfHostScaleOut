using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using Owin.Security.AesDataProtectorProvider;

namespace SignalRHost
{
    public static class Startup
    {
        public static void ConfigureApp(IAppBuilder app)
        {
            ConfigureCors(app);
            ConfigureSignalR(app);
            ConfigureStaticFiles(app);

            RunIncrementingTask();
        }

        private static void ConfigureCors(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
        }

        private static void ConfigureSignalR(IAppBuilder app)
        {
            app.UseAesDataProtectorProvider(SignalRScaleoutConfiguration.EncryptionPassword);

            if (SignalRScaleoutConfiguration.UseScaleout)
            {
                var redisConfig = new RedisScaleoutConfiguration(
                    SignalRScaleoutConfiguration.RedisConnectionString,
                    SignalRScaleoutConfiguration.RedisAppName);
                GlobalHost.DependencyResolver.UseRedis(redisConfig);
            }

            var configuration = new HubConfiguration { EnableDetailedErrors = true };
            app.MapSignalR(configuration);
        }

        private static void ConfigureStaticFiles(IAppBuilder app)
        {
            var fileSystem = new PhysicalFileSystem(@".\wwwroot");
            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                RequestPath = PathString.Empty,
                FileSystem = fileSystem,
            };

            options.DefaultFilesOptions.DefaultFileNames = new[] { "index.html" };
            options.StaticFileOptions.FileSystem = fileSystem;

            app.UseFileServer(options);
        }

        private static void RunIncrementingTask()
        {
            Task.Factory.StartNew(async () =>
            {
                var random = new Random();

                while (true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 2000)));

                    var message = $"Hello from node {FabricRuntime.GetNodeContext().NodeId}";

                    GlobalHost.ConnectionManager.GetHubContext<HelloHub>()
                        .Clients.All
                        .notifyNewMessage(message);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
