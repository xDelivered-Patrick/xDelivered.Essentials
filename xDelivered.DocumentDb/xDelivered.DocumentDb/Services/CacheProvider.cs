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

        public CacheProvider(string redisConnectionString, IDbContext docDb) : base(redisConnectionString, docDb)
        {

        }

        public bool Disconnect()
        {
            return true;
        }

        public async Task SetObject<T>(string key, T value, TimeSpan? expiry = null, bool updateUnderlying = false)
        {
            await _db.StringSetAsync(CacheHelper.CreateKey<T>(key), JsonConvert.SerializeObject(value), expiry: expiry);

            if (updateUnderlying)
            {
                var docDbRecord = value as DatabaseModelBase;
                if (docDbRecord != null)
                {
                    await base.DocumentDB.UpsertDocument(docDbRecord);
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
            await DocumentDB.UpsertDocument(applicationUser);
            await SetObject(CacheHelper.CreateKey(applicationUser, x=>x.Id), applicationUser);
        }

        public async Task AddToListAndTrim(string key, object item, int? limitByListCount = null)
        {
            await _db.SortedSetAddAsync(key, JsonConvert.SerializeObject(item), score: DateTime.UtcNow.Ticks);

            if (limitByListCount.HasValue)
            {
                await _db.SortedSetRemoveRangeByRankAsync(key, 0, -(limitByListCount.Value + 1));
            }
        }

        public Task AddToList<T>(string key, T item)
        {
            return _db.SortedSetAddAsync(key, JsonConvert.SerializeObject(item), score: DateTime.UtcNow.Ticks);
        }

        
        public async Task<bool> Exists<T>(string key, Func<Task<T>> func = null)
        {
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

        public new async Task<T> GetObjectOnlyCache<T>(string key)
        {
            try
            {
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
        
        public new Task<T> GetObject<T>(string id)
        {
            throw new NotImplementedException();
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