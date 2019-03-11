using Orleans;
using System.Threading.Tasks;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public interface IStringStorage : IGrainWithStringKey
    {
        Task Set(string value);
        Task<string> Get();
        Task ReadPersistentState();
        Task ClearPersistentState();
        Task ClearTransientState();
    }
}