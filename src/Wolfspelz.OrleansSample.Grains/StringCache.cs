using System.Threading.Tasks;
using Orleans;
using Wolfspelz.OrleansSample.GrainInterfaces;

namespace Wolfspelz.OrleansSample.Grains
{
    public class StringCache : Grain, IStringCache
    {
        private string _data = "";
        
        public Task Set(string value)
        {
            _data = value;
            return Task.CompletedTask;
        }

        public Task<string> Get()
        {
            return Task.FromResult(_data);
        }
    }
}