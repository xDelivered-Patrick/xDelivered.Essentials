using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using StackExchange.Redis;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Services
{
    public interface IXDbProvider : IDisposable
    {
        Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase;
        Task<bool> Exists<T>(string key);
        T GetObject<T>(string id);
        Task<T> GetObjectAsync<T>(string id);

        /// <summary>
        /// Will place an object into both Redis and CosmosDb
        /// </summary>
        /// <returns>ID of document</returns>
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


        /// <summary>
        /// Pulls an object straight from cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        T GetObjectOnlyCache<T>(string key);


        /// <summary>
        /// Place an object into Redis cache only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to place into cache</param>
        /// <param name="expiry">Optionally specify when object should expire</param>
        void SetObjectOnlyCache<T>(T obj, TimeSpan? expiry = null);


        /// <summary>
        /// Place an object into Redis cache only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to place into cache</param>
        /// <param name="expiry">Optionally specify when object should expire</param>
        void SetObjectOnlyCache<T>(string key, T obj, TimeSpan? expiry = null);

        /// <summary>
        /// Underlying Redis instance
        /// </summary>
        IDatabase RedisClient { get; }

        /// <summary>
        /// Underlying DocumentCosmosDb instance
        /// </summary>
        DocumentClient DocDbClient { get; }
    }
}