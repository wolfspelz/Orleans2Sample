using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using Orleans.Hosting;
using Wolfspelz.OrleansSample.GrainInterfaces;
using Wolfspelz.OrleansSample.Shared;

namespace Wolfspelz.OrleansSample.Client
{
    public class Program
    {
        private static string Mode { get; set; } = "Interactive"; // Test
        private static string ClusterId { get; set; } = Settings.ClusterId;
        private static string ServiceId { get; set; } = Settings.ServiceId;
        private static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        private static int MaxAttemptsBeforeFailing { get; set; } = 5;
        private static int AttemptDelaySec { get; set; } = 4;
        private static string SmsProviderName { get; set; } = Settings.SmsProviderName;

        private static int _attemptCounter = 0;

        static int Main(string[] args)
        {
            var q = new Queue<string>(args);
            while (q.Count > 0)
            {
                var arg = q.Dequeue().Trim();
                var parts = arg.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    switch (parts[0])
                    {
                        case "Mode": Mode = parts[1]; break;
                        case "ClusterId": ClusterId = parts[1]; break;
                        case "ServiceId": ServiceId = parts[1]; break;
                        case "ConnectionString": ConnectionString = parts[1]; break;
                        case "MaxAttemptsBeforeFailing": MaxAttemptsBeforeFailing = int.Parse(parts[1]); break;
                        case "AttemptDelaySec": AttemptDelaySec = int.Parse(parts[1]); break;
                    }
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
            async Task<bool> RetryFilter(Exception exception)
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
                //.ConfigureApplicationParts(x => x.AddApplicationPart(typeof(StringCacheGrain).Assembly).WithReferences())
                //.ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect(RetryFilter);
            Console.WriteLine("Connected to silo host");

            return client;
        }


        private static async Task DoClientWork(IClusterClient client)
        {
            switch (Mode)
            {
                case "Test":
                    DoTest(client);
                    break;
                default:
                    await DoInteractive(client);
                    break;
            }
        }

        private static void DoTest(IClusterClient client)
        {
            ExecuteTest(client, Test_BasicGrain);
            ExecuteTest(client, Test_Persistance);
            ExecuteTest(client, Test_Streams);
        }

        private static void ExecuteTest(IClusterClient client, Action<IClusterClient> action)
        {
            Console.WriteLine(action.Method.Name);
            try
            {
                action.Invoke(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void Test_Streams(IClusterClient client)
        {
            var producer = client.GetGrain<IStreamProducer>(Guid.NewGuid().ToString());
            var consumer1 = client.GetGrain<IStreamConsumer>(Guid.NewGuid().ToString());
            var consumer2 = client.GetGrain<IStreamConsumer>(Guid.NewGuid().ToString());
            var streamGuid = Guid.NewGuid();
            var streamName = "TestName";

            consumer1.Subscribe(streamGuid, streamName).Wait();

            producer.Send(streamGuid, streamName, "1").Wait();

            {
                var expected = "1";
                var result = consumer1.Get().Result;
                if (result != expected)
                {
                    throw new Exception($"Expected=<{expected}> result=<{result}>");
                }
            }

            consumer2.Subscribe(streamGuid, streamName).Wait();

            producer.Send(streamGuid, streamName, "2").Wait();

            {
                var expected = "12";
                var result = consumer1.Get().Result;
                if (result != expected) { throw new Exception($"Expected=<{expected}> result=<{result}>"); } }
            {
                var expected = "2";
                var result = consumer2.Get().Result;
                if (result != expected) { throw new Exception($"Late joiner: Expected=<{expected}> result=<{result}>"); }
            }

            consumer1.Unsubscribe().Wait();
            consumer2.Unsubscribe().Wait();
        }

        private static void Test_Persistance(IClusterClient client)
        {
            var id = Guid.NewGuid().ToString();
            //var id = "id1";
            var grain = client.GetGrain<IStringStorage>(id);

            var data = "Hello World";

            {
                grain.Set(data).Wait();
                var expected = data;
                var result = grain.Get().Result;
                if (result != expected) { throw new Exception($"Set/Get: Expected=<{expected}> result=<{result}>"); }
            }

            {
                grain.ClearTransientState().Wait();
                var expected = "";
                var result = grain.Get().Result;
                if (result != expected) { throw new Exception($"ClearTransient/Get: Expected=<{expected}> result=<{result}>"); }
            }

            {
                grain.ReadPersistentState().Wait();
                var expected = data;
                var result = grain.Get().Result;
                if (result != expected) { throw new Exception($"ReadPersistent/Get: Expected=<{expected}> result=<{result}>"); }
            }

            {
                grain.ClearPersistentState().Wait();
                grain.ClearTransientState().Wait();
                grain.ReadPersistentState().Wait();
                var expected = "";
                var result = grain.Get().Result;
                if (result != expected) { throw new Exception($"ClearPersistent/Get: Expected=<{expected}> result=<{result}>"); }
            }
        }

        private static void Test_BasicGrain(IClusterClient client)
        {
            var data = "Hello World";
            var grain = client.GetGrain<IStringCache>(Guid.NewGuid().ToString());
            grain.Set(data).Wait();
            var expected = data;
            var result = grain.Get().Result;
            if (result != expected) { throw new Exception($"Expected=<{expected}> result=<{result}>"); }
        }

        private static async Task DoInteractive(IClusterClient client)
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
                var grain = client.GetGrain<IStringStorage>(key);

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
