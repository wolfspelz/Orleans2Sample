using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Wolfspelz.OrleansSample.Grains;

// ReSharper disable once CheckNamespace
namespace Wolfspelz.OrleansSample.SiloHost
{
    public class Program
    {
        private static string ClusterId { get; set; } = "Demo";
        private static string ServiceId { get; set; } = "Sample";
        private static string ConnectionString { get; set; } = "";
        private static string MembershipTableConnectionString { get; set; } = "UseDevelopmentStorage=true";
        private static string GrainStateStoreConnectionString { get; set; } = "UseDevelopmentStorage=true";
        private static string PubSubStoreConnectionString { get; set; } = "UseDevelopmentStorage=true";
        private static string GrainStateBlobName { get; set; } = "sample-grains";
        private static string PubSubBlobName { get; set; } = "sample-pubsub";
        private static int GatewayPort { get; set; } = 2000;
        private static int SiloPort { get; set; } = 2001;

        public static int Main(string[] args)
        {
            ClusterId = Environment.GetEnvironmentVariable("ClusterId") ?? ClusterId;
            ServiceId = Environment.GetEnvironmentVariable("ServiceId") ?? ServiceId;
            ConnectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? ConnectionString;
            if (!string.IsNullOrEmpty(ConnectionString)) {
                MembershipTableConnectionString = ConnectionString;
                GrainStateStoreConnectionString = ConnectionString;
                PubSubStoreConnectionString = ConnectionString;
            }
            MembershipTableConnectionString = Environment.GetEnvironmentVariable("MembershipTableConnectionString") ?? MembershipTableConnectionString;
            GrainStateStoreConnectionString = Environment.GetEnvironmentVariable("GrainStateStoreConnectionString") ?? GrainStateStoreConnectionString;
            PubSubStoreConnectionString = Environment.GetEnvironmentVariable("PubSubStoreConnectionString") ?? PubSubStoreConnectionString;
            GrainStateBlobName = Environment.GetEnvironmentVariable("GrainStateBlobName") ?? GrainStateBlobName;
            PubSubBlobName = Environment.GetEnvironmentVariable("PubSubBlobName") ?? PubSubBlobName;
            GatewayPort = int.Parse(Environment.GetEnvironmentVariable("GatewayPort") ?? GatewayPort.ToString());
            SiloPort = int.Parse(Environment.GetEnvironmentVariable("SiloPort") ?? SiloPort.ToString());
            
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
                Console.ReadLine();
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = ClusterId;
                options.ServiceId = ServiceId;
            })
            .UseAzureStorageClustering(options =>
                options.ConnectionString = MembershipTableConnectionString
            )
            .ConfigureEndpoints(
                siloPort: SiloPort,
                gatewayPort: GatewayPort,
                hostname: IPAddress.Loopback.ToString()
            )
            .AddAzureBlobGrainStorage(Orleans.Providers.ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, options =>
            {
                options.ConnectionString = GrainStateStoreConnectionString;
                options.ContainerName = GrainStateBlobName;
            })
            .AddAzureBlobGrainStorage("PubSubStore", options =>
            {
                options.ConnectionString = PubSubStoreConnectionString;
                options.ContainerName = PubSubBlobName;
            })
            .AddSimpleMessageStreamProvider("default", (SimpleMessageStreamProviderOptions options) =>
            {
                options.FireAndForgetDelivery = true;
            })
            .UseInMemoryReminderService()
            .ConfigureApplicationParts(x =>
            {
                x.AddApplicationPart(typeof(StringCacheGrain).Assembly).WithReferences();
                x.AddApplicationPart(typeof(StringStorageGrain).Assembly).WithReferences();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            ;

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
