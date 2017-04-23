using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        protected IDbContext DocumentDB { get; }
        protected static IDatabase _db;
        private static ConnectionMultiplexer _redis;

        public XDbProvider(string redisConnectionString, IDbContext docDb)
        {
            DocumentDB = docDb;

            Connect(redisConnectionString);
        }

        protected void Connect(string con)
        {
            if (_db == null)
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,    // will not serialize an object if it is a child object of itself
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,  // is useful if objects are nested but not indefinitely
                    //PreserveReferencesHandling = PreserveReferencesHandling.Objects, // serialize an object that is nested indefinitely
                    TypeNameHandling = TypeNameHandling.None
                };

                var configurationOptions = ConfigurationOptions.Parse(con);
                configurationOptions.SyncTimeout = 30000;
                configurationOptions.AbortOnConnectFail = false;
                _redis = ConnectionMultiplexer.Connect(configurationOptions);
                _db = _redis.GetDatabase();
            }
        }
        
        public async Task<string> UpsertDocumentAndCache<T>(T value) where T : IDatabaseModelBase
        {
            Ensure.CheckForNull(value);
            
            //store into doc db
            string documentDbId = await DocumentDB.UpsertObject(value);

            //set Id of object, so redis will also have it
            value.Id = documentDbId;

            //create key for redis
            var redisKey = CacheHelper.CreateKey<T>(documentDbId);

            //use key to store into redis
            await _db.StringSetAsync(redisKey, JsonConvert.SerializeObject(value));

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
            RedisValue existing = _db.StringGet(CacheHelper.CreateKey<T>(key));
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
                    await _db.StringSetAsync(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);
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
            RedisValue existing = _db.StringGet(CacheHelper.CreateKey<T>(key));
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
                    _db.StringSet(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);
                }

                //return
                return value;
            }
        }

        public async Task<T> GetObjectAsync<T>(string id)
        {
            //first try cache
            T obj = await GetObjectOnlyCacheAsync<T>(id);
            
            if (obj == null)
            {
                //now try docdb
                obj = DocumentDB.GetDocument<T>(id);
            }

            if (obj == null)
            {
                return default(T);
            }

            return obj;
        }
        
        public Task<bool> Exists<T>(string key)
        {
            return _db.KeyExistsAsync(key);
        }

        public T GetObject<T>(string id)
        {
            //first try cache
            T obj = GetObjectOnlyCache<T>(id);

            if (obj == null)
            {
                //now try docdb
                obj = DocumentDB.GetDocument<T>(id);
            }

            if (obj == null)
            {
                return default(T);
            }

            return obj;
        }

        public async Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase
        {
            _db.KeyDelete(CacheHelper.CreateKey<T>(obj.Id));

            if (updateMasterDatabase)
            {
                await DocumentDB.DeleteDocument(obj);
            }
        }

        private async Task<T> GetObjectOnlyCacheAsync<T>(string key)
        {
            RedisValue json = await _db.StringGetAsync(CacheHelper.CreateKey<T>(key));

            if (json.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None});
            }
            return default(T);
        }


        public T GetObjectOnlyCache<T>(string key)
        {
            RedisValue json = _db.StringGet(CacheHelper.CreateKey<T>(key));

            if (json.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }

        public void SetObjectOnlyCache<T>(T obj, TimeSpan? expiry = null)
        {
            var key = Guid.NewGuid().ShortGuid();
            _db.StringSet(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(obj), expiry: expiry);
            RedisValue json = _db.StringGet(CacheHelper.CreateKey<T>(key));
            
            if (obj is IDatabaseModelBase)
            {
                var dbModel = obj as IDatabaseModelBase;
                dbModel.Id = key;
            }
        }

        public void Dispose()
        {
            _redis.Dispose();
        }
    }
}