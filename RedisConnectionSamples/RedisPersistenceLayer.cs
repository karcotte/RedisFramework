using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>RedisPersistenceLayer</c> provides an API for persisting DataDocuments in Redis.
    /// </summary>
    public class RedisPersistenceLayer
    {
        private static ILogger logger = ApplicationLogging.Factory.CreateLogger<RedisPersistenceLayer>();

        /// <summary>
        /// This method saves a single DataDocument to Redis at a <c>Location</c> within a particular schema. The Redis Key where the document
        /// is saved is determined as <code>schemaId:location</code>. All writes are "last write wins" when multiple writes to the same key are made at the same time.
        /// 
        /// Best practice is for all locations used within a "schemaId" to follow the same pattern.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document will be stored.</param>
        /// <param name="location">The location within the schema where the document will be stored.</param>
        /// <param name="doc">The <c>DataDocument</c> to persist in Redis.</param>
        /// <param name="expiration">The TTL for the key -> document pair. Defaults to null, indicating that there will not be a TTL.</param>
        /// <returns>A Task to track the asynchronous operation.</returns>
        public async Task Put(string schemaId, Location location, DataDocument doc, TimeSpan? expiration = null)
        {
            string key = schemaId + ":" + location;
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                await db.StringSetAsync(key, doc.ToRedisValue(), expiration);               
                logger.LogDebug("SET {key} {value}.", key, doc);
            });
        }

        /// <summary>
        /// This method saves a single DataDocument to Redis at a <c>Location</c> within a particular schema. The Redis Key where the document
        /// is saved is determined as <code>schemaId:location</code>. All writes are "last write wins" when multiple writes to the same key are made at the same time.
        /// 
        /// This is a blocking implementation, for non-blocking callers use "Put".
        /// 
        /// Best practice is for all locations used within a "schemaId" to follow the same pattern.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document will be stored.</param>
        /// <param name="location">The location within the schema where the document will be stored.</param>
        /// <param name="doc">The <c>DataDocument</c> to persist in Redis.</param>
        /// <param name="expiration">The TTL for the key -> document pair. Defaults to null, indicating that there will not be a TTL.</param>        
        public void PutSync(string schemaId, Location location, DataDocument doc, TimeSpan? expiration = null)
        {
            string key = schemaId + ":" + location;
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                db.StringSet(key, doc.ToRedisValue(), expiration);
                logger.LogDebug("SET {key} {value}.", key, doc);
            });
        }

        /// <summary>
        /// This method saves multiple DataDocuments to Redis at their corresponding <c>Location</c> within a particular schema. The Redis Key where the document[i]
        /// is saved is determined as <code>schemaId:location[i]</code>. All writes are "last write wins" when multiple writes to the same key are made at the same time.
        /// 
        /// Best practice is for all locations used wihin a "schemaId" to follow the same pattern.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document will be stored.</param>
        /// <param name="pairs">An array of location -> document pairs to be stored within the schema.</param>        
        /// <returns>A Task to track the asynchronous operation.</returns>
        public async Task Put(string schemaId, KeyValuePair<Location, DataDocument>[] pairs)
        {
            var keys = pairs.Select(pair => new RedisKey(schemaId + ":" + pair.Key.ToString())).ToArray();
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                var db = RedisConnection.Connection.GetDatabase();                
                await db.StringSetAsync(pairs.Select(pair => new KeyValuePair<RedisKey, RedisValue>(schemaId + ":" + pair.Key.ToString(), pair.Value.ToRedisValue())).ToArray());
                logger.LogDebug("MSET {pairs}", pairs);
            });
        }

        /// <summary>
        /// This method saves multiple DataDocuments to Redis at their corresponding <c>Location</c> within a particular schema. The Redis Key where the document[i]
        /// is saved is determined as <code>schemaId:location[i]</code>. All writes are "last write wins" when multiple writes to the same key are made at the same time.
        /// 
        /// This is a blocking implementaiton, for non-blocking callers use "Put".
        /// 
        /// Best practice is for all locations used wihin a "schemaId" to follow the same pattern.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document will be stored.</param>
        /// <param name="pairs">An array of location -> document pairs to be stored within the schema.</param>        
        /// <returns>A Task to track the asynchronous operation.</returns>
        public void PutSync(string schemaId, KeyValuePair<Location, DataDocument>[] pairs)
        {
            var keys = pairs.Select(pair => new RedisKey(schemaId + ":" + pair.Key.ToString())).ToArray();
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                db.StringSet(pairs.Select(pair => new KeyValuePair<RedisKey, RedisValue>(schemaId + ":" + pair.Key.ToString(), pair.Value.ToRedisValue())).ToArray());
                logger.LogDebug("MSET {pairs}", pairs);
            });
        }

        /// <summary>
        /// This method retrieves a document from Redis.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public async Task<DataDocument> Get(string schemaId, Location location)
        {
            var key = schemaId + ":" + location.ToString();
            DataDocument response = null;
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("GET {key}", key);
                response = new DataDocument(await db.StringGetAsync(key));                
            });
            return response;
        }

        /// <summary>
        /// This method retrieves a document from Redis.
        /// 
        /// This is a blocking implementation, for non-blocking use Get.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public DataDocument GetSync(string schemaId, Location location)
        {
            var key = schemaId + ":" + location.ToString();
            DataDocument response = null;
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("GET {key}", key);
                response = new DataDocument(db.StringGet(key));
            });
            return response;
        }

        /// <summary>
        /// This method retrieves the Locations within a schema matching a patten.
        /// </summary>
        /// <param name="schemaId">The id representing the schema.</param>
        /// <param name="location">A location pattern used to retrieve locations within the schema where documents are stored.</param>
        /// <returns>All of the locations within the schema where documents are stored.</returns>
        public IAsyncEnumerable<Location> Find(string schemaId, Location location)
        {
            var pattern = schemaId + ":" + location.ToString();
            var server = RedisConnection.Connection.GetServer(RedisConnection.Connection.GetEndPoints()[0]);
            logger.LogDebug("SCAN 0 MATCH {pattern}", pattern);
            //Note about the "Keys" call. StackExchange.Redis internally uses the SCAN operation if it is available on the server.
            //Recommendation is to disable KEYS entirely so that the client never attempts to use the blocking KEYS operation.
            //See: https://stackexchange.github.io/StackExchange.Redis/KeysScan.html
            return server.KeysAsync(pattern: pattern).Select(key => new Location(key));
        }

        /// <summary>
        /// This method deletes the document stored in Redis at a particular location within a schema.
        /// </summary>
        /// <param name="schemaId">The id representing the schema.</param>
        /// <param name="location">The location within the schema holding the document to be deleted.</param>
        /// <returns>A Task to track the asynchronous operation.</returns>
        public async Task Delete(string schemaId, Location location)
        {
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                //Note about the "Delete" call. StackExchange.Redis internally uses the UNLINK operation if it is available on the server.
                //See: https://github.com/StackExchange/StackExchange.Redis/commit/3dc56ba19f7dc8a8f667a05ddf202e2a1f321a8a
                await RedisConnection.Connection.GetDatabase().KeyDeleteAsync(schemaId + ":" + location.ToString());
            });
        }

        /// <summary>
        /// This method allows the caller to attempt to acquire a lock represented by setting a key in redis. Can only be set if the key
        /// does not already contain a value. Can be used for a pessimistic lock implementaiton.
        /// 
        /// See also: https://redis.io/topics/distlock
        /// </summary>
        /// <param name="key">The key representing a lock</param>
        /// <param name="token">A token known only to this caller, other callers will not be able to extend or remove the lock because they do not possess the token.</param>
        /// <param name="expiration">The TTL for the lock.</param>
        /// <returns>True if the lock was aquired, otherwise false.</returns>
        public Task<bool> acquireLock(string key, string token, TimeSpan expiration)
        {
            return RedisConnection.Connection.GetDatabase().LockTakeAsync(key, token, expiration);
        }

        /// <summary>
        /// This method allows the caller to extend the TTL for a lock that it already possess. The token must match the existing token stored in Redis at the key.
        /// </summary>
        /// <param name="key">The key representing the lock.</param>
        /// <param name="token">The token known only to the initial acquirer of the lock. Must be correct for the lock to be released.</param>
        /// <param name="expiration">The new TTL for the lock.</param>
        /// <returns>True if the lock was extended, otherwise false.</returns>
        public Task<bool> extendLock(string key, string token, TimeSpan expiration)
        {
            return RedisConnection.Connection.GetDatabase().LockExtendAsync(key, token, expiration);
        }

        /// <summary>
        /// This method allows the caller to release a lock so that other callers can acquire it.
        /// </summary>
        /// <param name="key">The key representing the lock.</param>
        /// <param name="token">The token known only to the initial acquirer of the lock. Must be correct for the lock to be released.</param>
        /// <returns>True if the lock was released, otherwise false.</returns>
        public Task<bool> releaseLock(string key, string token)
        {
            return RedisConnection.Connection.GetDatabase().LockReleaseAsync(key, token);
        }
    }
}
