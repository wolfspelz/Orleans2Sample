using System;
using System.Threading.Tasks;
using Orleans;
using Wolfspelz.OrleansSample.GrainInterfaces;
using Wolfspelz.OrleansSample.Shared;

namespace Wolfspelz.OrleansSample.Grains
{
    public class StreamProducerGrain : Grain, IStreamProducer
    {
        public async Task Send(Guid guid, string name, string data)
        {
            var sp = GetStreamProvider(Settings.SmsProviderName);
            var stream = sp.GetStream<string>(guid, name);
            await stream.OnNextAsync(data);
        }
    }
}