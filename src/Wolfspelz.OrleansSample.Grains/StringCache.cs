using System;
using System.Threading.Tasks;
using Orleans;
using Wolfspelz.OrleansSample.GrainInterfaces;

namespace Wolfspelz.OrleansSample.Grains
{
    public class StringCache : Grain, IStringCache
    {
        private string _data = "";

        private readonly Guid _streamId = Guid.NewGuid();
        public Task<Guid> GetStreamId() { return Task.FromResult(_streamId); }

        public async Task Set(string value)
        {
            _data = value;

            var streamProvider = GetStreamProvider(StringCacheStream.Provider);
            var stream = streamProvider.GetStream<string>(_streamId, StringCacheStream.Namespace);
            await stream.OnNextAsync(_data);
        }

        public async Task<string> Get()
        {
            await Task.CompletedTask;
            return _data;
        }
    }
}