using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Services
{
    public class DocumentDbContext : IDbCache
    {
        private readonly string _dbName;
        private readonly DocumentClient _client;

        private string Master = "main";

        public DocumentDbContext(string docDbEndpoint, string docDbkey, string dbName, string collection = "main")
        {
            _dbName = dbName;
            Master = collection;
            this._client = new DocumentClient(new Uri(docDbEndpoint), docDbkey,
                connectionPolicy: new ConnectionPolicy
                {
                    ConnectionMode = GetConnectionPolicy(),
                    ConnectionProtocol = Protocol.Tcp
                });

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None
            };
        }

        public ConnectionMode GetConnectionPolicy()
        {
            return ConnectionMode.Direct;
        }

        public async Task Init()
        {
            await CheckCreateDatabase();
            await CreateDocumentCollectionIfNotExists(Master);
        }


        public async Task<string> UpsertDocument<T>(T obj) where T : IDatabaseModelBase
        {
            Document doc = await this._client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, Master), obj, new RequestOptions() { });
            obj.Id = doc.Id;
            return doc.Id;
        }

        public async Task<string> UpsertObject(object obj)
        {
            Document doc = await this._client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, Master), obj);
            return doc.Id;
        }

        public async Task CheckCreateDatabase()
        {
            try
            {
                await this._client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_dbName));
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist, create a new database
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this._client.CreateDatabaseAsync(new Database { Id = _dbName });
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task CreateDocumentCollectionIfNotExists(string collectionName)
        {
            try
            {
                await this._client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_dbName, collectionName));
            }
            catch (DocumentClientException de)
            {
                // If the document collection does not exist, create a new collection
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    DocumentCollection collectionInfo = new DocumentCollection
                    {
                        Id = collectionName,
                        IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 })
                        {
                            //ingest now, query later
                            IndexingMode = IndexingMode.Lazy
                        }
                    };

                    //index on doctype
                    collectionInfo.IndexingPolicy.IncludedPaths.Add(
                        new IncludedPath
                        {
                            Path = "/DocType/?",
                            Indexes = new Collection<Index> {
                                new RangeIndex(DataType.String)
                                {
                                    Precision = 20
                                }
                            }
                        });

                    collectionInfo.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                    {
                        Path = "/\"Created\"/?",
                        Indexes = new Collection<Index> {
                                new RangeIndex(DataType.String)
                                {
                                    Precision = 7
                                }
                            }
                    });

                    await this._client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(_dbName),
                        collectionInfo,
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }

        public T GetDocument<T>(string id) where T : IDatabaseModelBase
        {
            try
            {
                var t = typeof(T).Name;

                return this._client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_dbName, Master))
                    .Where(x => x.Type == t && x.Id == id)
                    .AsEnumerable()
                    .FirstOrDefault();
            }
            catch (JsonSerializationException e)
            {
                Debug.WriteLine(e);
                return default(T);
            }
        }

        public Task<T> GetDocumentAsync<T>(string id) where T : IDatabaseModelBase
        {
            var t = typeof(T).Name;

            return Task.FromResult(this._client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_dbName, Master))
                .Where(x => x.Type == t && x.Id == id)
                .AsEnumerable()
                .FirstOrDefault());
        }

        public Task DeleteDocument<T>(T doc) where T : IDatabaseModelBase
        {
            return _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_dbName, Master, doc.Id));
        }
        

        public List<T> Search<T>(Func<T, bool> query) where T : IDatabaseModelBase
        {
            return this._client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_dbName, Master))
                .Where(x => x.Type == nameof(T))
                .Where(query)
                .ToList();
        }
        

        public List<T> GetDocuments<T>(Func<T, bool> query) where T : IDatabaseModelBase
        {
            var result = this._client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_dbName, Master))
                .Where(query);

            return result.ToList();
        }

        public async Task PurgeAll()
        {
            var db = _client.CreateDatabaseQuery().ToList().First();
            var coll = _client.CreateDocumentCollectionQuery(db.CollectionsLink).ToList().First();
            var docs = _client.CreateDocumentQuery(coll.DocumentsLink);
            foreach (var doc in docs)
            {
                await _client.DeleteDocumentAsync(doc.SelfLink);
            }
        }
    }
}
