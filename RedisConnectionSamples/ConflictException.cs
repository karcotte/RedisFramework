using System;
using System.Collections.Generic;
using System.Text;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>ConflictException</c> An exception representing that a resource change was detected. Used when implementing an optimistic locking mechanism.
    /// </summary>
    internal class ConflictException : Exception
    {
        public ConflictException()
        {

        }

        /// <summary>
        /// This constructor implements the exception with the given message.
        /// </summary>
        /// <param name="message">A message describing the error.</param>
        public ConflictException(string message) : base(message)
        {

        }
        /// <summary>
        /// This constructor implements the exception with the given message and inner exception.
        /// </summary>
        /// <param name="message">A message describing the error.</param>
        /// <param name="inner">An inner exception containing a stack trace indicating the root cause of this error.</param>
        public ConflictException(string message, Exception inner) : base (message, inner)
        {

        }
       
    }
}
