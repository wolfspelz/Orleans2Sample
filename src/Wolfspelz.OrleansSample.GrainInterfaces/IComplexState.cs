using Orleans;
using System.Threading.Tasks;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public interface IComplexState : IGrainWithStringKey
    {
        Task SaveState();
        Task<bool> CheckState();
    }
}