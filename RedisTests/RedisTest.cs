using NUnit.Framework;
using RedisConnectionSamples;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTests
{
    public class RedisTest
    {
        private RedisPersistenceLayer redis;
        private ConnectionMultiplexer connection;

        public RedisTest()
        {
            //Must set an environment variable for the "connection_string" we take care of this in docker compose.
            //Environment.SetEnvironmentVariable("connection_string", "redis:12000,connectRetry=3,connectTimeout=3000,abortConnect=false");
            ThreadPool.SetMinThreads(200, 200);
            redis = new RedisPersistenceLayer();

            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(Environment.GetEnvironmentVariable("connection_string"), true);
            redisConfig.AbortOnConnectFail = false;
            redisConfig.AllowAdmin = true;
            redisConfig.CommandMap = CommandMap.Create(new HashSet<string>() { "KEYS", "DEL" }, available: false);
            connection = ConnectionMultiplexer.Connect(redisConfig);
        }

        [SetUp]
        public void Setup()
        {
            connection.GetServer(connection.GetEndPoints()[0]).FlushAllDatabases();
        }

        [Test]
        public async Task TestDocumentPut()
        {            
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");            
            await redis.Put("unit-test", "1", doc, TimeSpan.FromSeconds(600));
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (await redis.Get("unit-test", "1")).GetFirstValueAsString("field1"));
        }

        [Test]
        public async Task TestGrid()
        {
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

            await redis.Put("grid-test", pairs);

            var firstRowDocs = await redis.Find("grid-test", new string[] { "0", "*" }).ToArrayAsync();
            Assert.AreEqual(3, firstRowDocs.Length);

            var secondRowDocs = await redis.Find("grid-test", new string[] { "1", "*" }).ToArrayAsync();
            Assert.AreEqual(3, secondRowDocs.Length);
        }

        [Test]
        public async Task TestCities()
        {
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

            await redis.Put("cities-test", pairs);
            
            var usDocs = await redis.Find("cities-test", new string[] { "United States", "*", "*" }).ToArrayAsync();
            Assert.AreEqual(5, usDocs.Length);

            var nyDocs = await redis.Find("cities-test", new string[] { "United States", "NY", "*" }).ToArrayAsync();
            Assert.AreEqual(4, nyDocs.Length);

            var maDocs = await redis.Find("cities-test", new string[] { "United States", "MA", "*" }).ToArrayAsync();
            Assert.AreEqual(1, maDocs.Length);
        }

        [Test]
        public async Task testApplicationLock()
        {
            Assert.IsTrue(await redis.acquireLock("lock-test", "abc", TimeSpan.FromMinutes(10)), "1: expected lock to be acquired.");
            Assert.IsFalse(await redis.acquireLock("lock-test", "abc", TimeSpan.FromMinutes(10)), "2: expected lock to not be acquired.");
            Assert.IsFalse(await redis.extendLock("lock-test", "123", TimeSpan.FromMinutes(10)), "3: expected lock to not be extended");
            Assert.IsTrue(await redis.extendLock("lock-test", "abc", TimeSpan.FromMinutes(10)), "4: expected lock to be extended");
            Assert.IsTrue(await redis.releaseLock("lock-test", "abc"), "5: expected lock to be released");
            Assert.IsTrue(await redis.acquireLock("lock-test", "abc", TimeSpan.FromMinutes(10)), "6: expected lock to be acquired.");
            Assert.IsTrue(await redis.releaseLock("lock-test", "abc"), "7: expected lock to be released.");
        }

        [Test]
        public void TestSyncPut()
        {
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");
            redis.PutSync("unit-test", "1", doc, TimeSpan.FromSeconds(600));
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (redis.GetSync("unit-test", "1")).GetFirstValueAsString("field1"));
        }
    }
}