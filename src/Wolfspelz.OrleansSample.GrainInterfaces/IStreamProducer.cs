using System;
using Orleans;
using System.Threading.Tasks;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public interface IStreamProducer : IGrainWithStringKey
    {
        Task Send(Guid guid, string name, string data);
    }
}