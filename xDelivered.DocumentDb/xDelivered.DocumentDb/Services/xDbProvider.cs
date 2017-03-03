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
    public class XDbProvider : IXDbProvider
    {
        protected IDbContext DocumentDB { get; }
        private IDatabase _db;

        public XDbProvider(string redisConnectionString, IDbContext docDb)
        {
            DocumentDB = docDb;

            Connect(redisConnectionString);
        }

        private void Connect(string con)
        {
            if (_db == null)
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,    // will not serialize an object if it is a child object of itself
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,  // is useful if objects are nested but not indefinitely
                    //PreserveReferencesHandling = PreserveReferencesHandling.Objects, // serialize an object that is nested indefinitely
                    TypeNameHandling = TypeNameHandling.All
                };

                var configurationOptions = ConfigurationOptions.Parse(con);
                configurationOptions.SyncTimeout = 30000;
                var _redis = ConnectionMultiplexer.Connect(configurationOptions);
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
        
        public async Task<T> GetObject<T>(string id)
        {
            //first try cache
            T obj = await GetObjectOnlyCache<T>(id);
            
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

        public async Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase
        {
            _db.KeyDelete(CacheHelper.CreateKey<T>(obj.Id));

            if (updateMasterDatabase)
            {
                await DocumentDB.DeleteDocument(obj);
            }
        }

        private async Task<T> GetObjectOnlyCache<T>(string key)
        {
            RedisValue json = await _db.StringGetAsync(CacheHelper.CreateKey<T>(key));

            if (json.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }
    }
}