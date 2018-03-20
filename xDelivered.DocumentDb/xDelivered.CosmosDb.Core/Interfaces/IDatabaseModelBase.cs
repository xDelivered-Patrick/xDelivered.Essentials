using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace xDelivered.CosmosDb.Core
{
    public interface IDatabaseModelBase
    {
        [JsonProperty("id")]
        string Id { get; set; }
        DateTime Created { get; set; }
        DateTime? Updated { get; set; }
        string Type { get; }
        bool IsDeleted { get; set; }
    }


    public interface ICosmosDbProvider
    {
        DocumentClient Client { get; }
        ConnectionMode GetConnectionPolicy();
        Task Init();
        Task<string> UpsertDocument<T>(T obj) where T : IDatabaseModelBase;
        Task<string> UpsertObject(object obj);
        Task CheckCreateDatabase();
        Task CreateDocumentCollectionIfNotExists(string collectionName);
        T GetDocument<T>(string id);
        Task<T> GetDocumentAsync<T>(string id) where T : IDatabaseModelBase;
        Task DeleteDocument<T>(T doc) where T : IDatabaseModelBase;
        List<T> Search<T>(Func<T, bool> query) where T : IDatabaseModelBase;
        Task PurgeAll();
        IOrderedQueryable<T> NewQuery<T>(FeedOptions options = null) where T : IDatabaseModelBase;
        IQueryable<T> Query<T>() where T : IDatabaseModelBase;
    }
}