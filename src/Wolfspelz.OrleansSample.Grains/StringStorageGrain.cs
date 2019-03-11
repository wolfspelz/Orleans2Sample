using System.Threading.Tasks;
using Orleans;
using Wolfspelz.OrleansSample.GrainInterfaces;

namespace Wolfspelz.OrleansSample.Grains
{
    public class StringStorageState
    {
        public string Data { get; set; }
    }

    public class StringStorageGrain : Grain<StringStorageState>, IStringStorage
    {        
        public async Task Set(string value)
        {
            if (value != State.Data)
            {
                State.Data = value;
                await base.WriteStateAsync();
            }
        }

        public Task<string> Get()
        {
            return Task.FromResult(State.Data);
        }

        public async Task ReadPersistentState()
        {
            await base.ReadStateAsync();
        }

        public async Task ClearPersistentState()
        {
            await base.ClearStateAsync();
        }

        public Task ClearTransientState()
        {
            State.Data = "";
            return Task.CompletedTask;
        }
    }
}