using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace xDelivered.DocumentDb.Interfaces
{
    interface IDbCache
    {
        ConnectionMode GetConnectionPolicy();
        Task Init();
        Task<string> UpsertDocument<T>(T obj) where T : IDatabaseModelBase;
        Task<string> UpsertObject(object obj);
        Task CheckCreateDatabase();
        Task CreateDocumentCollectionIfNotExists(string collectionName);
        T GetDocument<T>(string id) where T : IDatabaseModelBase;
        Task<T> GetDocumentAsync<T>(string id) where T : IDatabaseModelBase;
        Task DeleteDocument<T>(T doc) where T : IDatabaseModelBase;
        List<T> Search<T>(Func<T, bool> query) where T : IDatabaseModelBase;
        List<T> GetDocuments<T>(Func<T, bool> query) where T : IDatabaseModelBase;
        Task PurgeAll();
    }
}
