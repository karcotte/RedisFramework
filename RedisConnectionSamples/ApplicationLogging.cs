
using Microsoft.Extensions.Logging;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>ApplicationLogging</c> provides a sample Microsoft.Extensions.Logging implementation configured to write to console and windows event log.
    /// </summary>
    class ApplicationLogging
    {
        private static ILoggerFactory _Factory = null;

        /// <summary>
        /// Property <c>Factory</c> provides a logger factory implementation that allows the rest of the applicaiton to use the configured logging implementation.
        /// </summary>
        public static ILoggerFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    _Factory = LoggerFactory.Create(builder =>
                    {                        
                        builder.AddConsole();      
                        //event log is not available from docker images
                        //builder.AddEventLog();
                    });
                }
                return _Factory;
            }
        }

    }
}
