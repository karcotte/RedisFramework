using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>RetryHelper</c> provides an asynchronous mechanism for retrying an execution that accesses a transient resource.
    /// </summary>
    public static class RetryHelper
    {
        private static ILogger Log = ApplicationLogging.Factory.CreateLogger("RetryHelper");

        /// <summary>
        /// This method retries an operation until it is successful or until the number of attempts is exceeded. 
        /// This implementation only retries for certain exceptions. When the underlying operation throws a 
        /// <c>RedisException</c>, or <c>SocketException</c> the operation will be tried again. Any other error 
        /// will be rethrown without additional attempts.
        /// </summary>
        /// <param name="times">The number of attempts allowed.</param>
        /// <param name="operation">The operation to retry.</param>
        /// <returns></returns>
        public static async Task RetryOnExceptionAsync(
            int times, Func<Task> operation)
        {
            if (times <= 0)
                throw new ArgumentOutOfRangeException(nameof(times));

            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    await operation();
                    break;
                }
                catch (Exception ex)
                {
                    if(ex is RedisException || ex is SocketException)
                    {
                        if (attempts == times)
                            throw;
                        await CreateDelayForException(times, attempts, ex);
                    } else
                    {
                        throw;
                    }

                }
            } while (true);
        }

        private static Task CreateDelayForException(
            int times, int attempts, Exception ex)
        {
            var _delay = IncreasingDelayInSeconds(attempts);
            Log.LogWarning($"Exception on attempt {attempts} of {times}. " + "Will retry after sleeping for {delay}.", ex);
            return Task.Delay(_delay);
        }

        internal static int[] DelayPerAttemptInSeconds =
        {
            (int) TimeSpan.FromSeconds(1).TotalSeconds,
            (int) TimeSpan.FromSeconds(2).TotalSeconds,
            (int) TimeSpan.FromSeconds(5).TotalSeconds,
        };

        static int IncreasingDelayInSeconds(int failedAttempts)
        {
            if (failedAttempts <= 0) throw new ArgumentOutOfRangeException();

            return failedAttempts > DelayPerAttemptInSeconds.Length ? DelayPerAttemptInSeconds.Last() : DelayPerAttemptInSeconds[failedAttempts];
        }
    }
}
