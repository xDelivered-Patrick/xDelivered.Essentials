using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using xDelivered.Common;
using xDelivered.DocumentDb.Helpers;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Services
{
    public class CacheProvider : XDbProvider, ICacheProvider
    {
        private readonly string _redisConnectionString;
        private readonly IDbContext _docDb;
        private IDatabase _db;
        private ConnectionMultiplexer _redis;

        public CacheProvider(string redisConnectionString, IDbContext docDb) : base(redisConnectionString, docDb)
        {
            _redisConnectionString = redisConnectionString;
            _docDb = docDb;

            Connect();
        }

        public void Connect()
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

                var configurationOptions = ConfigurationOptions.Parse(_redisConnectionString);
                configurationOptions.SyncTimeout = 30000;
                _redis = ConnectionMultiplexer.Connect(configurationOptions);
                _db = _redis.GetDatabase();
            }
        }

        public bool Disconnect()
        {
            _db = null;
            return true;
        }

        public async Task SetObject<T>(string key, T value, TimeSpan? expiry = null, bool updateUnderlying = false)
        {
            Connect();

            await _db.StringSetAsync(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);

            if (updateUnderlying)
            {
                var docDbRecord = value as DatabaseModelBase;
                if (docDbRecord != null)
                {
                    await _docDb.UpsertDocument(docDbRecord);
                }
            }
        }

        public Task SetObject<T>(T model, TimeSpan expiry, bool updateMasterDb = true) where T : IDatabaseModelBase
        {
            return SetObject(model.Id, model, expiry, updateMasterDb);
        }

        public Task<long> GetListCount(string key)
        {
            return _db.SortedSetLengthAsync(key);
        }

        public async Task RemoveFromList(string key, string value)
        {
            await _db.SortedSetRemoveAsync(key, JsonConvert.SerializeObject(value));
        }
        
        public Task ClearList(string setId)
        {
            return _db.KeyDeleteAsync(setId);
        }

        public async Task UpdateUser(IDatabaseModelBase applicationUser)
        {
            await _docDb.UpsertDocument(applicationUser);
            await SetObject(CacheHelper.CreateKey(applicationUser, x=>x.Id), applicationUser);
        }

        public async Task AddToListAndTrim(string key, object item, int? limitByListCount = null)
        {
            Connect();

            await _db.SortedSetAddAsync(key, JsonConvert.SerializeObject(item), score: DateTime.UtcNow.Ticks);

            if (limitByListCount.HasValue)
            {
                await _db.SortedSetRemoveRangeByRankAsync(key, 0, -(limitByListCount.Value + 1));
            }
        }

        public Task AddToList<T>(string key, T item)
        {
            Connect();

            return _db.SortedSetAddAsync(key, JsonConvert.SerializeObject(item), score: DateTime.UtcNow.Ticks);
        }

        
        public async Task<bool> Exists<T>(string key, Func<Task<T>> func = null)
        {
            Connect();

            if (func != null)
            {
                var cacheValue = await GetOrCreateAsync(key, func);

                return cacheValue != null;
            }
            else
            {
                return _db.KeyExists(key);
            }

        }

        public async Task<T> GetObjectOnlyCache<T>(string key)
        {
            try
            {
                Connect();

                RedisValue json = await _db.StringGetAsync(CacheHelper.CreateKey<T>(key));

                if (json.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                return default(T);
            }
            catch (JsonException e)
            {
                Debug.WriteLine(e);
                return default(T);
            }
        }


        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> func, TimeSpan? expiry = null)
        {
            Connect();

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

        public T GetOrCreate<T>(string key, Func<T> func, TimeSpan? expiry = null)
        {
            Connect();

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

       
        public async Task<List<T>> GetSortedSetAsync<T>(string redisKey)
        {
            List<T> result = new List<T>();
            RedisValue[] memebers = await _db.SortedSetRangeByScoreAsync(redisKey);

            foreach (var redisValue in memebers)
            {
                if (redisValue.HasValue)
                {
                    result.Add(JsonConvert.DeserializeObject<T>(redisValue));
                }
            }

            return result;
        }

        public List<T> GetSortedSet<T>(string redisKey)
        {
            List<T> result = new List<T>();
            RedisValue[] memebers = _db.SortedSetRangeByScore(redisKey);

            foreach (var redisValue in memebers)
            {
                if (redisValue.HasValue)
                {
                    result.Add(JsonConvert.DeserializeObject<T>(redisValue));
                }
            }

            return result;
        }
    }
}