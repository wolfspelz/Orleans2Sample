using Orleans;
using System.Threading.Tasks;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public interface IStringCache : IGrainWithStringKey
    {
        Task Set(string value);
        Task<string> Get();
    }
}