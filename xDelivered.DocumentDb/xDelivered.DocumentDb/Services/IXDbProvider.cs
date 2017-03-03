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
        Task<T> GetObject<T>(string id);
        Task<string> UpsertDocumentAndCache<T>(T value) where T : IDatabaseModelBase;
    }
}