using System;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Services
{
    public interface IXDbProvider
    {
        Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase;
        Task<bool> Exists<T>(string key);
        T GetObject<T>(string id);
        Task<T> GetObjectAsync<T>(string id);
        Task<string> UpsertDocumentAndCache<T>(T value) where T : IDatabaseModelBase;

        /// <summary>
        /// Will attempt to pull from Redis. If no match will call the Func (where to pull the value from) and will store in redis and return. 
        /// 
        /// Note : primarily used for DocDbRedisCacheResolver
        /// </summary>
        /// <typeparam name="T">Type of document to return</typeparam>
        /// <param name="objectId">expected key of the document</param>
        /// <param name="create">where to get the value from if no match</param>
        /// <param name="expiry">when the cache value should expire in redis</param>
        /// <returns>The document</returns>
        T GetOrCreate<T>(string objectId, Func<T> create, TimeSpan? expiry = null); 

        /// <summary>
        /// Will attempt to pull from Redis. If no match will call the Func (where to pull the value from) and will store in redis and return. 
        /// 
        /// Note : primarily used for DocDbRedisCacheResolver
        /// </summary>
        /// <typeparam name="T">Type of document to return</typeparam>
        /// <param name="objectId">expected key of the document</param>
        /// <param name="create">where to get the value from if no match</param>
        /// <param name="expiry">when the cache value should expire in redis</param>
        /// <returns>The document</returns>
        Task<T> GetOrCreateAsync<T>(string objectId, Func<Task<T>> create, TimeSpan? expiry = null);

        T GetObjectOnlyCache<T>(string key);
    }
}