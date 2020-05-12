using System;
using System.Threading.Tasks;
using Orleans;

namespace Wolfspelz.OrleansSample.GrainInterfaces
{
    public class StringCacheStream
    {
        public const string Provider = "SMSProvider";
        public const string Namespace = "Changes";
    }

    public interface IStringCache : IGrainWithStringKey
    {
        Task Set(string value);
        Task<string> Get();
        Task<Guid> GetStreamId();
    }
}