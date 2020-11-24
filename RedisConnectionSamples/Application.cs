using System;
using System.Collections.Generic;
using System.Text;

namespace RedisConnectionSamples
{
    class Application
    {
        static void Main(string [] args)
        {
            RedisPersistenceLayer redis = new RedisPersistenceLayer();
            RedisHashPersistenceLayer hashes = new RedisHashPersistenceLayer();
            DataDocument data = new DataDocument();
            data.Set("name", "John Doe");
            data.Set("age", 84);
            data.Add("items", new object[] { 1, 2, 3, "fast", "slow", 90.3, true, false });
            redis.PutSync("person", "1", data);
            hashes.PutSync("person-hash", "1", data);
            Console.WriteLine("data before persistence: " + data);            
            Console.WriteLine("data persisted as string: " + redis.GetSync("person", "1"));
            Console.WriteLine("data persisted as hash: " + hashes.GetSync("person-hash", "1"));
            Console.WriteLine("items retrieved from hash field: " + hashes.GetSync("person-hash", "1", "items"));
            Console.WriteLine("name and age retrieved from hash fields: " + hashes.GetSync("person-hash", "1", new string[] { "name", "age" }));
        }
    }
}
