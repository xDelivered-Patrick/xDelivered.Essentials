# xDelivered XDb Provider

#### What is this?

  >Data access service combining CosmodDb and Redis for optimised latency and NoSQL relationships


#### Why is this useful?
- Query via cosmosDb for enumerable and complex queries
- Pull from Redis for simple object grabs
- Optimised latency for type of query, leveraging Redis or Cosmos depending on requirements
- Framework allowing relationships between NoSQL document types

**Upsert documents to Redis and CosmosDb in one line**
```csharp
//use DI for this in production
var db = new XDbProvider("redisConnectionString", new CosmosContext("cosmosEndpoint", "cosmosKey", "cosmosDb", "mainCollection"));

//insert into cosmos and redis
await db.UpsertDocumentAndCache(new User {FirstName = "Billy", LastName = "Bob", Age = 35});
```
**Pull single object with cache fallback**
```csharp
//pulls from database (tries Redis first, falls back to CosmosProvider)
var user = await db.GetObjectAsync<User>("userId");
```
**Push to cache only with expiry**
```csharp
_db.SetObjectOnlyCache(user, TimeSpan.FromHours(3));
```

**Automagically handles ID creation, Created/Updated, Soft deletes**
```json
{
  "id": "Billy Bob",
  "FirstName": "Billy",
  "LastName": "Bob",
  "FullName": "Billy Bob",
  "Age": 35,
  "Created": "2017-05-23T11:05:02.6059438Z",
  "Updated": null,
  "Type": "User",
  "IsDeleted": false
}
```

**Single object location - uses object links to define noSQL relationships**
```csharp
public class User : DatabaseModelBase
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public int Age { get; set; }

    public List<ObjectLink<Tournament>> CompletedTournaments { get; set; } = new List<ObjectLink<Tournament>>();
}

//add link between user and tournament
user.CompletedTournaments.Add(new ObjectLink<Tournament>(completedTournament));
```
Which stores JSON with foreign relationship link

```json
{
  "Name": "Andy Murray",
  "CompletedTournaments": [
    {
      "Link": "nhwcvpzjf05tyj4rjmq",
      "Identifier": "nhwcvpzjf05tyj4rjmq"
    }
  ]
}
```
Now resolve foreign relationship in single line via object resolver. Leverages Memory/Redis/Cosmos for optimal latency
```csharp
//now pull tournament from user object via memory/redis/cosmosDb (order of query)
Tournament tourney = user.CompletedTournaments.First().Resolve();
```

**Override key creation to achieve object upsert persistance instead of using short GUID**
```csharp
public class User : DatabaseModelBase
{
    public override string Id => $"{FirstName} {LastName}";

    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

![Redis2](https://s30.postimg.org/w0dlfqy81/redis2.png)

### How do I run the samples?
You'll need local running instances of Redis and Cosmos.

1. Install chocolatly
   > https://chocolatey.org/install
2. Install redis
   > choco install redis
3. Install CosmosDb locally
   > choco install azure-documentdb-emulator
4. Run!

### Recommended tools
- https://redisdesktop.com/
- http://documentdb.a7.plus/

### Hire me
 - www.xdelivered.com
