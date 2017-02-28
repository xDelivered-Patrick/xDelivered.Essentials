using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Interfaces
{
    public interface ICacheProvider
    {
        Task AddToList<T>(string key, T item);
        Task AddToListAndTrim(string key, object item, int? limitByListCount = default(int?));
        void Connect();
        Task DeleteObject<T>(T obj, bool updateMasterDatabase = true) where T : IDatabaseModelBase;
        bool Disconnect();
        Task<bool> Exists<T>(string key, Func<Task<T>> func = null);
        Task<T> GetObject<T>(string id) where T : IDatabaseModelBase;
        Task<T> GetObjectOnlyCache<T>(string key);
        T GetOrCreate<T>(string key, Func<T> func, TimeSpan? expiry = default(TimeSpan?));
        Task<T> GetOrCreateAsync<T>(string objectId, Func<Task<T>> func, TimeSpan? expiry = default(TimeSpan?));
        List<T> GetSortedSet<T>(string redisKey);
        Task<List<T>> GetSortedSetAsync<T>(string redisKey);
        Task SetObject<T>(T model, TimeSpan expiry, bool updateMasterDb = true) where T : IDatabaseModelBase;
        Task SetObject<T>(string key, T value, TimeSpan? expiry = default(TimeSpan?), bool updateUnderlying = false);
        Task<string> UpsertDocumentAndCache<T>(T value) where T : DatabaseModelBase;
        Task<long> GetListCount(string key);
        Task RemoveFromList(string key, string value);
        Task ClearList(string setId);
    }
}