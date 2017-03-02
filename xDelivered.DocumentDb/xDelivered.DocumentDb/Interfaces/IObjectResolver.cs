using System.Threading.Tasks;

namespace xDelivered.DocumentDb.Interfaces
{
    public interface IObjectResolver
    {
        Task<T> ResolveAsync<T>(string id) where T : IDatabaseModelBase;
        T Resolve<T>(string id) where T : IDatabaseModelBase;
    }
}