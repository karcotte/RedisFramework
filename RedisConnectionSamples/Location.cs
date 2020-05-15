using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>Location</c> models a part of a key used within a schema to store documents.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Instance variable representing the indice parts of the location which can later be searched for with a pattern.
        /// </summary>
        string[] indices;

        /// <summary>
        /// Property <c>Length</c> represents the number of indices present in the Location.
        /// </summary>
        public int Length
        {
            get { return indices.Length; }
        }

        /// <summary>
        /// This constructor initializes the location with the indices in the <paramref name="indices"/> array.
        /// </summary>
        /// <param name="indices">The string array used to intialize the location.</param>
        public Location(params string[] indices)
        {
            this.indices = indices;
        }

        /// <summary>
        /// Converts a string into a location.
        /// </summary>
        /// <param name="index"></param>
        public static implicit operator Location(string index)
        {
            return new Location(index.Split(':'));
        }

        /// <summary>
        /// Converts a string array into a location.
        /// </summary>
        /// <param name="indices"></param>
        public static implicit operator Location(string[] indices)
        {
            return new Location(indices);
        }

        /// <summary>
        /// Converts an integer into a location.
        /// </summary>
        /// <param name="index"></param>
        public static implicit operator Location(int index)
        {
            return new Location(index.ToString());
        }


        /// <summary>
        /// Converts an integer array into a location.
        /// </summary>
        /// <param name="indices"></param>
        public static implicit operator Location(int[] indices)
        {
            return new Location(indices.Select(index => index.ToString()).ToArray());
        }        

        /// <summary>
        /// This method returns a string representation of the location.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < indices.Length; i++)
            {
                if(i != 0)
                {
                    sb.Append(":");
                }
                sb.Append(indices[i]);
            }
            return sb.ToString();
        }
    }
}
