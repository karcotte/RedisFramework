# C# Framework for basic Redis use cases

This framework uses the StackExchange.Redis library to implement basic redis use cases 

The framework handles:
- Creating arbitrary documents and persisting them in Redis
- Retrieving a document by its key
- Retrieving documents by arbitrary patterns defined by a schema
- Deleting documents by key
- A pessimistic locking mechanism that allows applications to acquire, extend, and release locks based on a redis key.
- A configurable retry mechanism for operations that interact with Redis

All of the persistence used cases are implemented with a asynchronous method calls, taking advantage of StackExchange.Redis's MultiPlexer and ability to pipeline requests to provide a continuation based interface.

## Examples

Create a U.S. cities based schema and persist information about each city's population. Include country and state in the schema so cities 
stored in the system can be looked up by state or country.

```C#
DataDocument rochester = new DataDocument();            
rochester.Set("population", 208880);
DataDocument syracuse = new DataDocument();            
syracuse.Set("population", 142749);
DataDocument buffalo = new DataDocument();            
buffalo.Set("population", 256902);
DataDocument binghamton = new DataDocument();            
binghamton.Set("population", 45672);
DataDocument boston = new DataDocument();           
boston.Set("population", 694583);

KeyValuePair<Location, DataDocument>[] pairs = new KeyValuePair<Location, DataDocument>[]
{
    new KeyValuePair<Location, DataDocument>(new string[]{ "United States", "NY", "Rochester"}, rochester),
    new KeyValuePair<Location, DataDocument>(new string[] { "United States", "NY", "Syracuse" }, syracuse),
    new KeyValuePair<Location, DataDocument>(new string[] { "United States", "NY", "Buffalo" }, buffalo),
    new KeyValuePair<Location, DataDocument>(new string[] { "United States", "NY", "Binghamton" }, binghamton),
    new KeyValuePair<Location, DataDocument>(new string[] { "United States", "MA", "Boston" }, boston)
};
//
await redis.Put("cities", pairs);
//Use the schema to find particular cities by country, or by state.        
var usDocs = await redis.Find("cities", new string[] { "United States", "*", "*" }).ToArrayAsync();
var nyDocs = await redis.Find("cities", new string[] { "United States", "NY", "*" }).ToArrayAsync();
var maDocs = await redis.Find("cities", new string[] { "United States", "MA", "*" }).ToArrayAsync();
var rochester = await redis.Get("cities", new string[] {"United States", "NY", "Rochester"});
```

Create a two dimensional grid based schema storing scores at various geographic points in the grid.

```C#
DataDocument doc1 = new DataDocument();
doc1.Set("score", 95);
DataDocument doc2 = new DataDocument();
doc2.Set("score", 87);
DataDocument doc3 = new DataDocument();
doc3.Set("score", 81);
DataDocument doc4 = new DataDocument();
doc4.Set("score", 71);
DataDocument doc5 = new DataDocument();
doc5.Set("score", 99);
DataDocument doc6 = new DataDocument();
doc6.Set("score", 16);

KeyValuePair<Location, DataDocument>[] pairs = new KeyValuePair<Location, DataDocument>[]
{
    new KeyValuePair<Location, DataDocument>(new int[]{ 0, 0}, doc1),
    new KeyValuePair<Location, DataDocument>(new int[] { 0, 1 }, doc2),
    new KeyValuePair<Location, DataDocument>(new int[] { 0, 2 }, doc3),
    new KeyValuePair<Location, DataDocument>(new int[] { 1, 0 }, doc4),
    new KeyValuePair<Location, DataDocument>(new int[] { 1, 1 }, doc5),
    new KeyValuePair<Location, DataDocument>(new int[] { 1, 2 }, doc6),
};

await redis.Put("grid", pairs);

var firstRowDocs = await redis.Find("grid", new string[] { "0", "*" }).ToArrayAsync();
var secondRowDocs = await redis.Find("grid", new string[] { "1", "*" }).ToArrayAsync();
var firstCell = await redis.Get("grid", new int[] {0, 0});
```

Acquire a pessimistic lock

```C#
await redis.acquireLock("lock-test", "abc", TimeSpan.FromMinutes(10)) // 1: expected lock to be acquired
await redis.acquireLock("lock-test", "abc", TimeSpan.FromMinutes(10)) // 2: expected lock to not be acquired
await redis.extendLock("lock-test", "123", TimeSpan.FromMinutes(10)) // 3: expected lock to not be extended
await redis.extendLock("lock-test", "abc", TimeSpan.FromMinutes(10)) // 4: expected lock to be extended
await redis.releaseLock("lock-test", "abc") // expected lock to be released
await redis.acquireLock("lock-test", "abc") // expected lock to be acquired
await redis.releaseLock("lock-test", "abc") // expected lock to be released
```

## Implementation

This framework uses the [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) library. 