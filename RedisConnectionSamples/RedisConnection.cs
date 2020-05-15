using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>RedisConnection</c> provides the framework with a means of accessing a configured connection to communicate with a redis database. Connection is determined by the
    /// "connection_string" environment variable.
    /// 
    /// See Also: https://stackexchange.github.io/StackExchange.Redis/Configuration
    /// </summary>
    internal class RedisConnection
    {       
        private static string connectionString;
        private static readonly object connectionStringLock = new object();

        private static ILogger logger = ApplicationLogging.Factory.CreateLogger<RedisConnection>();
        private static string ConnectionString
        {
            get
            {
                if (connectionString == null)
                {
                    lock (connectionStringLock)
                    {
                        if (connectionString == null)
                        {
                            connectionString = Environment.GetEnvironmentVariable("connection_string");
                        }
                    }
                }
                return connectionString;
            }             
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = CreateMultiplexer();

        /// <summary>
        /// Property <c>Connection</c> is a lazy initialized singleton StackExchange.Redis ConnectionMultiplexer used by the framework to communicate with a Redis database.
        /// </summary>
        internal static ConnectionMultiplexer Connection
        {
            get { return lazyConnection.Value; }
        }

        private static Lazy<ConnectionMultiplexer> CreateMultiplexer()
        {
            return new Lazy<ConnectionMultiplexer>(() =>
            {                
                ConfigurationOptions redisConfig = ConfigurationOptions.Parse(ConnectionString, true);
                redisConfig.AbortOnConnectFail = false;
                redisConfig.AllowAdmin = false;
                redisConfig.CommandMap = CommandMap.Create(new HashSet<string>() { "KEYS", "DEL" }, available: false);
                ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(redisConfig);
                connection.ConnectionFailed += c_ConnectionFailed;
                connection.ConnectionRestored += c_ConnectionRestored;
                logger.LogDebug("Multiplexer is created.");
                return connection;
            });
        }

        private static void c_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogError(e.Exception, "The redis connection has failed. {{endpoint:{endpoint}, failureType:{failureType}, connectionType:{connectionType}}}", e.EndPoint, e.FailureType, e.ConnectionType);
        }

        private static void c_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogInformation("The redis connection has been restored. {{endpoint:{endpoint}, connectionType:{connectionType}}}", e.EndPoint, e.ConnectionType);
        }
    }
}
