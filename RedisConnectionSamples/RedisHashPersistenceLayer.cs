using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>RedisHashPersistenceLayer</c> provides an API for persisting DataDocuments as Hashes in Redis.
    /// </summary>
    public class RedisHashPersistenceLayer
    {
        private static ILogger logger = ApplicationLogging.Factory.CreateLogger<RedisHashPersistenceLayer>();

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
                await db.HashSetAsync(key, doc.ToHashEntries());                
                logger.LogDebug("SET {key} {value}.", key, doc);
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
                logger.LogDebug("HGETALL {key}", key);
                response = new DataDocument(await db.HashGetAllAsync(key));
            });
            return response;
        }

        /// <summary>
        /// This method retrieves a subset of a document from Redis containing specific fields.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// <param name="fields">The fields to be retrieved.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public async Task<DataDocument> Get(string schemaId, Location location, string[] fields)
        {
            var key = schemaId + ":" + location.ToString();
            DataDocument response = null;
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("HGET {key} {fields}", key, fields);
                response = new DataDocument(fields.Zip(await db.HashGetAsync(key, Array.ConvertAll(fields, field => (RedisValue)field)), (field, value) => new HashEntry(field, value)).ToArray());
            });
            return response;
        }

        /// <summary>
        /// This method retrieves a document from Redis.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// /// <param name="field">The field to be retrieved.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public async Task<string> Get(string schemaId, Location location, string field)
        {
            var key = schemaId + ":" + location.ToString();
            string response = null;
            await RetryHelper.RetryOnExceptionAsync(3, async () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("GET {key} {field}", key, field);
                response = await db.HashGetAsync(key, field);
            });
            return response;
        }

        /// <summary>
        /// This method saves a single DataDocument to Redis at a <c>Location</c> within a particular schema. The Redis Key where the document
        /// is saved is determined as <code>schemaId:location</code>. All writes are "last write wins" when multiple writes to the same key are made at the same time.
        /// 
        /// Best practice is for all locations used within a "schemaId" to follow the same pattern.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document will be stored.</param>
        /// <param name="location">The location within the schema where the document will be stored.</param>
        /// <param name="doc">The <c>DataDocument</c> to persist in Redis.</param>        
        /// <returns>A Task to track the asynchronous operation.</returns>
        public void PutSync(string schemaId, Location location, DataDocument doc)
        {
            string key = schemaId + ":" + location;
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                db.HashSet(key, doc.ToHashEntries());
                logger.LogDebug("SET {key} {value}.", key, doc);
            });
        }

        /// <summary>
        /// This method retrieves a document from Redis.
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
                response = new DataDocument(db.HashGetAll(key));
            });
            return response;
        }

        /// <summary>
        /// This method retrieves a document from Redis.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// <param name="fields">The fields to be retrieved.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public DataDocument GetSync(string schemaId, Location location, string[] fields)
        {
            var key = schemaId + ":" + location.ToString();
            DataDocument response = null;
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("GET {key}", key);
                response = new DataDocument(fields.Zip(db.HashGet(key, Array.ConvertAll(fields, field => (RedisValue)field)), (field, value) => new HashEntry(field, value)).ToArray());
            });
            return response;
        }

        /// <summary>
        /// This method retrieves a document from Redis.
        /// </summary>
        /// <param name="schemaId">The id representing the schema where the document is stored.</param>
        /// <param name="location">The location within the schema where the document is stored.</param>
        /// <param name="field">The field to be retrieved.</param>
        /// <returns>A Task to track the asynchronous retrieval operation.</returns>
        public object GetSync(string schemaId, Location location, string field)
        {
            var key = schemaId + ":" + location.ToString();
            string response = null;
            RetryHelper.RetryOnException(3, () =>
            {
                var db = RedisConnection.Connection.GetDatabase();
                logger.LogDebug("GET {key}", key);
                response = db.HashGet(key, field);                
            });
            return response;
        }
    }
}
