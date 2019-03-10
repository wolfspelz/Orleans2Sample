using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Wolfspelz.OrleansSample.Grains;

namespace Wolfspelz.OrleansSample.SiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "Sample";
            })
            .UseAzureStorageClustering(options => 
                options.ConnectionString = "UseDevelopmentStorage=true"
            )
            .ConfigureEndpoints(
                siloPort: 15411,
                gatewayPort: 15412,
                hostname: IPAddress.Loopback.ToString()
            )
            .AddAzureBlobGrainStorage("default", options =>
            {
                options.ConnectionString = "UseDevelopmentStorage=true";
                options.ContainerName = "sample-grains";
            })
            .AddAzureBlobGrainStorage("PubSubStore", options =>
            {
                options.ConnectionString = "UseDevelopmentStorage=true";
                options.ContainerName = "sample-pubsub";
            })
            .AddSimpleMessageStreamProvider("default", (SimpleMessageStreamProviderOptions options) =>
            {
                options.FireAndForgetDelivery = true;
            })
            .UseInMemoryReminderService()
            .ConfigureApplicationParts(x =>
            {
                x.AddApplicationPart(typeof(StringCacheGrain).Assembly).WithReferences();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            ;

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
