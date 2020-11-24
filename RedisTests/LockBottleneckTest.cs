using NUnit.Framework;
using RedisConnectionSamples;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTests
{
    class LockBottleneckTest
    {
        private ConnectionMultiplexer connection;
        protected static Dictionary<string, object> _localCache = new Dictionary<string, object>();

        public LockBottleneckTest()
        {
            //Must set an environment variable for the "connection_string" we take care of this in docker compose.
            //Environment.SetEnvironmentVariable("connection_string", "redis:12000,connectRetry=3,connectTimeout=3000,abortConnect=false");
            ThreadPool.SetMinThreads(200, 200);
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
            connection.GetDatabase().StringSet("hello", "world");           
        }

        [Test]
        public async Task TestLocalCacheLockedGet()
        {
            var getTasks = new List<Task>();
            for(var i = 0; i < 100; i++)
            {
                var getTask = Task.Run(() => lockedGet());
                getTasks.Add(getTask);
            }
            await Task.WhenAll(getTasks);
            Console.WriteLine("Locked Get Tasks Complete!");
        }

        [Test]
        public async Task TestGet()
        {
            var getTasks = new List<Task>();
            for (var i = 0; i < 100; i++)
            {
                var getTask = Task.Run(() => Get());
                getTasks.Add(getTask);
            }
            await Task.WhenAll(getTasks);
            Console.WriteLine("Get Tasks Complete!");
        }

        private string lockedGet()
        {
            var db = connection.GetDatabase();
            lock (((IDictionary)_localCache).SyncRoot)
            {                
                return db.StringGet("hello");
            }
        }

        private string Get()
        {
            var db = connection.GetDatabase();
            return db.StringGet("hello");
        }
    }
}

