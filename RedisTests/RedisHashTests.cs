using StackExchange.Redis;
using RedisConnectionSamples;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;

namespace RedisTests
{
    class RedisHashTests
    {
        private RedisHashPersistenceLayer redis;
        private ConnectionMultiplexer connection;

        public RedisHashTests()
        {
            //Must set an environment variable for the "connection_string" we take care of this in docker compose.
            //Environment.SetEnvironmentVariable("connection_string", "redis:12000,connectRetry=3,connectTimeout=3000,abortConnect=false");
            ThreadPool.SetMinThreads(200, 200);
            redis = new RedisHashPersistenceLayer();

            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(Environment.GetEnvironmentVariable("connection_string"), true);
            redisConfig.AbortOnConnectFail = false;
            redisConfig.AllowAdmin = true;
            redisConfig.CommandMap = CommandMap.Create(new HashSet<string>() { "KEYS", "DEL" }, available: false);
            connection = ConnectionMultiplexer.Connect(redisConfig);
        }

        [Test]
        public async Task TestDocumentPut()
        {
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");
            await redis.Put("unit-test-hash", "1", doc);
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (await redis.Get("unit-test-hash", "1")).GetFirstValueAsString("field1"));
        }

        [Test]
        public async Task TestDocumentGetByFields()
        {
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");
            await redis.Put("unit-test-hash", "1", doc);
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (await redis.Get("unit-test-hash", "1", new string[] { "field1" })).GetFirstValueAsString("field1"));
        }

        [Test]
        public void TestDocumentPutSync()
        {
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");
            redis.PutSync("unit-test-hash", "1", doc);
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (redis.GetSync("unit-test-hash", "1")).GetFirstValueAsString("field1"));
        }

        [Test]
        public void TestDocumentGetByFieldsSync()
        {
            DataDocument doc = new DataDocument();
            doc.Set("field1", "value1");
            redis.PutSync("unit-test-hash", "1", doc);
            Assert.AreEqual(doc.GetFirstValueAsString("field1"), (redis.GetSync("unit-test-hash", "1", new string[] { "field1" })).GetFirstValueAsString("field1"));
        }
    }
}
