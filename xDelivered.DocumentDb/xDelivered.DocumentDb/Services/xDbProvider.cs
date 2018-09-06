using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using StackExchange.Redis;
using xDelivered.Common;
using xDelivered.DocumentDb.Helpers;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Services
{
    public class XDbProvider : IXDbProvider, IDisposable
    {
        protected ICosmosDb DocumentCosmosDb { get; }
        protected static IDatabase Db;
        private static ConnectionMultiplexer _redis;

        /// <summary>
        /// Underlying DocumentCosmosDb instance
        /// </summary>
        public DocumentClient DocDbClient => DocumentCosmosDb.Client;

        /// <summary>
        /// Underlying Redis instance
        /// </summary>
        public IDatabase RedisClient => Db;

        /// <summary>
        /// Cached Resolver for easy access
        /// </summary>
        public static IObjectResolver Resolver { get; set; }

        public XDbProvider(string redisConnectionString, ICosmosDb cosmosCosmosDb)
        {
            DocumentCosmosDb = cosmosCosmosDb;

            Connect(redisConnectionString);
            Resolver = new DocDbRedisResolver(this);
        }

        /// <summary>
        /// Makes sure collections are created. Only do this once per solution to ensure performance.
        /// </summary>
        /// <returns></returns>
        public Task Init()
        {
            return this.DocumentCosmosDb.Init();
        }

        protected void Connect(string con)
        {
            if (Db != null) return;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,  // is useful if objects are nested but not indefinitely
                TypeNameHandling = TypeNameHandling.None
            };

            var configurationOptions = ConfigurationOptions.Parse(con);
            configurationOptions.SyncTimeout = 30000;
            configurationOptions.AbortOnConnectFail = false;
            _redis = ConnectionMultiplexer.Connect(configurationOptions);
            Db = _redis.GetDatabase();
        }

        /// <summary>
        /// Will place an object into both Redis and CosmosDb
        /// </summary>
        /// <returns>ID of document</returns>
        public async Task<string> UpsertDocumentAndCache<T>(T value) where T : IDatabaseModelBase
        {
            //insert into cosmos
            var documentDbId = await UpsertDocumentOnly(value);

            //create key for redis
            var redisKey = CacheHelper.CreateKey<T>(documentDbId);

            //use key to store into redis
            await Db.StringSetAsync(redisKey, JsonConvert.SerializeObject(value));

            return documentDbId;
        }

        public async Task<string> UpsertDocumentOnly<T>(T value) where T : IDatabaseModelBase
        {
            Ensure.CheckForNull(value);

            value.id = CacheHelper.RemoveKeyPrefixes(value.Id);

            //store into doc db
            string documentDbId = await DocumentCosmosDb.UpsertDocument(value);

            //set Id of object, so redis will also have it
            value.Id = documentDbId;

            return documentDbId;
        }

        /// <summary>
        /// Will attempt to pull from Redis. If no match will call the Func (where to pull the value from) and will store in redis and return. 
        /// 
        /// Note : primarily used for DocDbRedisCacheResolver
        /// </summary>
        /// <typeparam name="T">Type of document to return</typeparam>
        /// <param name="key">expected key of the document</param>
        /// <param name="func">where to get the value from if no match</param>
        /// <param name="expiry">when the cache value should expire in redis</param>
        /// <returns>The document</returns>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> func, TimeSpan? expiry = null)
        {
            //is the value in redis?
            RedisValue existing = Db.StringGet(CacheHelper.CreateKey<T>(key));
            if (existing.HasValue && !existing.IsNullOrEmpty)
            {
                //yes, return
                return JsonConvert.DeserializeObject<T>(existing);
            }
            else
            {
                //no, create the value
                T value = await func();

                if (value != null)
                {
                    //store in redis
                    await Db.StringSetAsync(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);
                }

                //return
                return value;
            }
        }


        /// <summary>
        /// Will attempt to pull from Redis. If no match will call the Func (where to pull the value from) and will store in redis and return. 
        /// 
        /// Note : primarily used for DocDbRedisCacheResolver
        /// </summary>
        /// <typeparam name="T">Type of document to return</typeparam>
        /// <param name="key">expected key of the document</param>
        /// <param name="func">where to get the value from if no match</param>
        /// <param name="expiry">when the cache value should expire in redis</param>
        /// <returns>The document</returns>
        public T GetOrCreate<T>(string key, Func<T> func, TimeSpan? expiry = null)
        {
            //is the value in redis?
            RedisValue existing = Db.StringGet(CacheHelper.CreateKey<T>(key));
            if (existing.HasValue && !existing.IsNullOrEmpty)
            {
                //yes, return
                return JsonConvert.DeserializeObject<T>(existing);
            }
            else
            {
                //no, create the value
                T value = func();

                if (value != null)
                {
                    //store in redis
                    Db.StringSet(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);
                }

                //return
                return value;
            }
        }

        /// <summary>
        /// Retrieves an object from Redis first, then DocumentCosmosDb afterwards
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T> GetObjectAsync<T>(string id)
        {
            //first try cache
            T obj = await GetObjectOnlyCacheAsync<T>(id);

            if (obj == null)
            {
                //now try docdb
                obj = DocumentCosmosDb.GetDocument<T>(id);
            }

            if (obj == null)
            {
                return default(T);
            }

            return obj;
        }

        /// <summary>
        /// Does exist in Redis?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> Exists<T>(string key)
        {
            return Db.KeyExistsAsync(key);
        }

        /// <summary>
        /// Retrieves an object from Redis first, then DocumentCosmosDb afterwards
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetObject<T>(string id)
        {
            //first try cache
            T obj = GetObjectOnlyCache<T>(id);

            if (obj == null)
            {
                //now try docdb
                obj = DocumentCosmosDb.GetDocument<T>(id);
            }

            if (obj == null)
            {
                return default(T);
            }

            return obj;
        }

        /// <summary>
        /// Delete from cache, optionally delete from Db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="updateMasterDatabase"></param>
        /// <returns></returns>
        public async Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase
        {
            Db.KeyDelete(CacheHelper.CreateKey<T>(obj.Id));

            if (updateMasterDatabase)
            {
                await DocumentCosmosDb.DeleteDocument(obj);
            }
        }

        private async Task<T> GetObjectOnlyCacheAsync<T>(string key)
        {
            RedisValue json = await Db.StringGetAsync(CacheHelper.CreateKey<T>(key));

            if (json.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None });
            }
            return default(T);
        }

        /// <summary>
        /// Pulls an object straight from cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetObjectOnlyCache<T>(string key)
        {
            RedisValue json = Db.StringGet(CacheHelper.CreateKey<T>(key));

            if (json.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }

        /// <summary>
        /// Place an object into Redis cache only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to place into cache</param>
        /// <param name="expiry">Optionally specify when object should expire</param>
        public void SetObjectOnlyCache<T>(T obj, TimeSpan? expiry = null)
        {
            SetObjectOnlyCache(Guid.NewGuid().ShortGuid(), obj, expiry);
        }


        /// <summary>
        /// Place an object into Redis cache only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to place into cache</param>
        /// <param name="expiry">Optionally specify when object should expire</param>
        public void SetObjectOnlyCache<T>(string key, T obj, TimeSpan? expiry = null)
        {
            if (obj is IDatabaseModelBase)
            {
                var dbm = (obj as IDatabaseModelBase);
                if (dbm.Id.IsNotNullOrEmpty())
                {
                    key = dbm.Id;
                }
            }

            Db.StringSet(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(obj), expiry: expiry);
            
            if (obj is IDatabaseModelBase dbModel)
            {
                if (dbModel.Id.IsNullOrEmpty())
                {
                    dbModel.Id = key;
                }
            }
        }


        /// <summary>
        /// Place an object into Redis cache only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object to place into cache</param>
        /// <param name="expiry">Optionally specify when object should expire</param>
        public void SetObjectOnlyCache(string key, object obj, TimeSpan? expiry = null)
        {
            Db.StringSet(key, JsonConvert.SerializeObject(obj), expiry: expiry);
        }

        public void Dispose()
        {

        }
    }
}