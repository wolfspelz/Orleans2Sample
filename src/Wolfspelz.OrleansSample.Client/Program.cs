using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using Orleans.Hosting;
using Wolfspelz.OrleansSample.GrainInterfaces;
using Wolfspelz.OrleansSample.Grains;

namespace Wolfspelz.OrleansSample.Client
{
    public class Program
    {
        private static string ClusterId { get; set; } = "Demo";
        private static string ServiceId { get; set; } = "Sample";
        private static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        private static int MaxAttemptsBeforeFailing { get; set; } = 5;
        private static int AttemptDelaySec { get; set; } = 4;

        private static int _attemptCounter = 0;

        static int Main(string[] args)
        {
            var q = new Queue<string>(args);
            while (q.Count > 0)
            {
                var arg = q.Dequeue();
                arg = arg.Trim();
                switch (arg)
                {
                    case "--help":
                        break;
                    default:
                        var kv = arg.Split(new[] { '=' }, 2);
                        if (kv.Length == 2)
                        {
                            switch (kv[0])
                            {
                                case "ClusterId": ClusterId = kv[1]; break;
                                case "ServiceId": ServiceId = kv[1]; break;
                                case "ConnectionString": ConnectionString = kv[1]; break;
                                case "MaxAttemptsBeforeFailing": MaxAttemptsBeforeFailing = int.Parse(kv[1]); break;
                                case "AttemptDelaySec": AttemptDelaySec = int.Parse(kv[1]); break;
                            }
                        }
                        break;
                }
            }

            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    await DoClientWork(client);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries()
        {
            _attemptCounter = 0;
            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = ClusterId;
                    options.ServiceId = ServiceId;
                })
                .UseAzureStorageClustering(options =>
                    options.ConnectionString = ConnectionString
                )
                .ConfigureApplicationParts(x => x.AddApplicationPart(typeof(StringCacheGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect(RetryFilter);
            Console.WriteLine("Client successfully connect to silo host");

            return client;
        }

        private static async Task<bool> RetryFilter(Exception exception)
        {
            if (exception.GetType() != typeof(SiloUnavailableException))
            {
                Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                return false;
            }
            _attemptCounter++;
            Console.WriteLine($"Cluster client attempt {_attemptCounter} of {MaxAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");
            if (_attemptCounter > MaxAttemptsBeforeFailing)
            {
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(AttemptDelaySec));
            return true;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
            Console.WriteLine("[set|get|inc] key (value)");

            while (true)
            {
                var line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"Terminating");
                    return;
                }

                var parts = line.Split(" ", 3, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 1)
                {
                    Console.WriteLine($"Terminating");
                    return;
                }

                var action = parts[0];

                if (action == "quit" || action == "q" || parts.Length < 2)
                {
                    Console.WriteLine($"Terminating");
                    return;
                }

                var key = parts[1];
                var grain = client.GetGrain<IStringCache>(key);

                switch (action)
                {
                    case "set":
                        {
                            var value = parts[2];
                            await grain.Set(value);
                            Console.WriteLine($"{key} <- {value}");
                        }
                        break;

                    case "get":
                        {
                            var value = await grain.Get();
                            Console.WriteLine($"{key} -> {value}");
                        }
                        break;

                    case "inc":
                        {
                            var value = await grain.Get();
                            long.TryParse(value, out long num);
                            num++;
                            await grain.Set(num.ToString());
                            Console.WriteLine($"{key} <- {num}");
                        }
                        break;
                }
            }
        }
    }
}
