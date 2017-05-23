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
    public class CosmosProvider : ICosmosDb
    {
        private readonly string _dbName;
        private readonly DocumentClient _client;

        protected string Collection { get; set; } = "main";
        protected Uri CollectionUri => UriFactory.CreateDocumentCollectionUri(_dbName, Collection);

        public CosmosProvider(string docDbEndpoint, string docDbkey, string dbName, string collection = "main")
        {
            _dbName = dbName;
            Collection = collection;
            this._client = new DocumentClient(new Uri(docDbEndpoint), docDbkey,
                connectionPolicy: new ConnectionPolicy
                {
                    ConnectionMode = GetConnectionPolicy(),
                    ConnectionProtocol = Protocol.Tcp
                });


            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,  // is useful if objects are nested but not indefinitely
                                                                          //PreserveReferencesHandling = PreserveReferencesHandling.Objects, // serialize an object that is nested indefinitely
                TypeNameHandling = TypeNameHandling.None
            };
        }

        protected DocumentClient Client => _client;

        DocumentClient ICosmosDb.Client => Client;

        public ConnectionMode GetConnectionPolicy()
        {
            return ConnectionMode.Direct;
        }

        public IOrderedQueryable<T> NewQuery<T>() where T : IDatabaseModelBase
        {
            return _client.CreateDocumentQuery<T>(CollectionUri);
        }
        public async Task Init()
        {
            await CheckCreateDatabase();
            await CreateDocumentCollectionIfNotExists(Collection);
        }


        public async Task<string> UpsertDocument<T>(T obj) where T : IDatabaseModelBase
        {
            Document doc = await this._client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, Collection), obj);
            obj.Id = doc.Id;
            return doc.Id;
        }

        public async Task<string> UpsertObject(object obj)
        {
            Document doc = await this._client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, Collection), obj);
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
                            Path = $"/{nameof(IDatabaseModelBase.Type)}/?",
                            Indexes = new Collection<Index> {
                                new RangeIndex(DataType.String)
                                {
                                    Precision = 20
                                }
                            }
                        });

                    collectionInfo.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                    {
                        Path = $"/\"{nameof(IDatabaseModelBase.Created)}\"/?",
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

        public T GetDocument<T>(string id)
        {
            try
            {
                Document docJson = this._client.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri(_dbName, Collection))
                    .Where(x => x.Id == id)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (docJson == null) return default(T);

                var result = JsonConvert.DeserializeObject<T>(docJson.ToString());
                return result;

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

            return Task.FromResult(this._client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_dbName, Collection))
                .Where(x => x.Type == t && x.Id == id)
                .AsEnumerable()
                .FirstOrDefault());
        }

        public Task DeleteDocument<T>(T doc) where T : IDatabaseModelBase
        {
            return _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_dbName, Collection, doc.Id));
        }
        

        public List<T> Search<T>(Func<T, bool> query) where T : IDatabaseModelBase
        {
            return this._client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_dbName, Collection))
                .Where(x => x.Type == nameof(T))
                .Where(query)
                .ToList();
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
