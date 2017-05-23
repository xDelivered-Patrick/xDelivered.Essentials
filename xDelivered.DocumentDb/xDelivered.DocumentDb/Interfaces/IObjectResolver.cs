using System.Threading.Tasks;

namespace xDelivered.DocumentDb.Interfaces
{
    /// <summary>
    /// A resolver for entity relationships between Documentmodels. 
    /// </summary>
    public interface IObjectResolver
    {
        /// <summary>
        /// Will resolve an object from cache/db based on ID
        /// </summary>
        Task<T> ResolveAsync<T>(string id) where T : IDatabaseModelBase;

        /// <summary>
        /// Will resolve an object from cache/db based on ID
        /// </summary>
        T Resolve<T>(string id) where T : IDatabaseModelBase;
    }
}