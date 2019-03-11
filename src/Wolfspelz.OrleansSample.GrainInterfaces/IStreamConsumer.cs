using System;
using Orleans;
using System.Threading.Tasks;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public interface IStreamConsumer : IGrainWithStringKey
    {
        Task Set(string value);
        Task<string> Get();
        Task Subscribe(Guid guid, string name);
        Task Unsubscribe();
    }
}