using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using Wolfspelz.OrleansSample.GrainInterfaces;
using Wolfspelz.OrleansSample.Shared;

namespace Wolfspelz.OrleansSample.Grains
{
    public class StreamConsumerGrain : Grain, IStreamConsumer, IAsyncObserver<string>
    {
        private string _data = "";
        private StreamSubscriptionHandle<string> _subscription;
        
        public Task Set(string value)
        {
            _data = value;
            return Task.CompletedTask;
        }

        public Task<string> Get()
        {
            return Task.FromResult(_data);
        }

        public async Task Subscribe(Guid guid, string name)
        {
            var sp = GetStreamProvider(Settings.SmsProviderName);
            var stream = sp.GetStream<string>(guid, name);
            _subscription = await stream.SubscribeAsync(this);
        }

        public async Task Unsubscribe()
        {
            if (_subscription != null)
            {
                await _subscription.UnsubscribeAsync();
            }
        }

        public Task OnNextAsync(string item, StreamSequenceToken token = null)
        {
            _data += item;
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}