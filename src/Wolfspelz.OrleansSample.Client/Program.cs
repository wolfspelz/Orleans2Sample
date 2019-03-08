using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using Wolfspelz.OrleansSample.GrainInterfaces;

namespace Wolfspelz.OrleansSample.Client
{
    public class Program
    {
        const int initializeAttemptsBeforeFailing = 5;
        private static int attempt = 0;

        static int Main(string[] args)
        {
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
            attempt = 0;
            IClusterClient client;
            client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
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
            attempt++;
            Console.WriteLine($"Cluster client attempt {attempt} of {initializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");
            if (attempt > initializeAttemptsBeforeFailing)
            {
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(4));
            return true;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
            Console.WriteLine("test1");
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
