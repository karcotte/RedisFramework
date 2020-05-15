using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace RedisConnectionSamples
{
    /// <summary>
    /// Class <c>DataDocument</c> models a document containing data that can be persisted in Redis.
    /// </summary>
    public class DataDocument
    {
        /// <summary>
        /// Instance variable <c>dataMap</c> represents the backing dictionary holding the document's data.
        /// </summary>
        private Dictionary<string, object> dataMap;
        
        /// <summary>
        /// This constructor initializes a new empty DataDocument.
        /// </summary>
        public DataDocument()
        {
            this.dataMap = new Dictionary<string, object>();           
        }

        /// <summary>
        /// This constructor initializes a new DataDocument backed by the 
        /// (<paramref name="dataMap"/>).
        /// </summary>
        /// <param name="dataMap"> is the new DataDocument's backing dictionary.</param>
        public DataDocument(Dictionary<string, object> dataMap)
        {
            this.dataMap = dataMap;
        }

        /// <summary>
        /// This constructor initializes a new DataDocument with the values contained in the incoming 
        /// (<paramref name="jsonValue"/>).
        /// </summary>
        /// <param name="jsonValue"> is the DataDocument's UTF-8 encoded JSON representation.</param>
        internal DataDocument(byte[] jsonValue)
        {
            this.dataMap = JsonSerializer.Deserialize<Dictionary<string, object>>(new ReadOnlySpan<byte>(jsonValue));
        }

        /// <summary>
        /// This method saves the (<paramref name="value"/>) to the (<paramref name="field"/>) field in the DataDocument. 
        /// If a value or values already exists in the DataDocument at that field, the new value replaces the old value(s)
        /// </summary>
        /// <param name="field"> is the DataDocument field that the (<paramref name="value"/>) will be saved to.</param>
        /// <param name="value"> will be saved to the (<paramref name="field"/>) field.</param>
        public void Set(string field, object value)
        {
            dataMap[field] = value;       
        }
    
        /// <summary>
        /// This method adds the (<paramref name="value"/>) to the (<paramref name="field"/>) field in the DataDocument.
        /// If a value or values already exists in the DataDocument at that field, the new value is appended to the old value(s).
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void Add(string field, object value)
        {
            if(dataMap.ContainsKey(field))
            {
                object currentValue = dataMap[field];
                if(currentValue is IEnumerable<object>)
                {                    
                    ((IEnumerable<object>)currentValue).Append(value);                
                } else
                {
                    List<object> values = new List<object>();
                    values.Add(currentValue);                                        
                    dataMap[field] = values;
                }
            } else
            {
                List<object> values = new List<object>();                
                dataMap[field] = values;
            }
        }

        /// <summary>
        /// This method adds the (<paramref name="value"/>) to the (<paramref name="field"/>) field in the DataDocument.
        /// If a value or values already exist in the DataDocument at that field, the values contained in the
        /// (<paramref name="value"/>) are appended to the old value(s). 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void Add(string field, IEnumerable<object> value)
        {
            if(dataMap.ContainsKey(field))
            {
                object currentValue = dataMap[field];
                if(currentValue is IEnumerable<object>)
                {
                    ((IEnumerable<object>)currentValue).Concat(value);
                } else
                {
                    List<object> values = new List<object>();
                    values.Add(currentValue);
                    values.AddRange(value);
                    dataMap[field] = values;
                }
            } else
            {
                List<object> values = new List<object>();
                values.AddRange(value);
            }
        }

        /// <summary>
        /// This method retrieves the first value in the DataDocument stored in the (<paramref name="field"/>) field.
        /// </summary>
        /// <param name="field"> is the field to retrieve the first value from.</param>
        /// <returns>The first value stored at the field in the DataDocument or null if the field doesn't exist.</returns>
        public object GetFirstValue(string field)
        {
            return this.GetFirstValue(field, null);
        }

        /// <summary>
        /// This method retrieves the first value in the DataDocument stored in the <paramref name="field"/> field.
        /// </summary>
        /// <param name="field"> is the field to retireve the first value from.</param>
        /// <param name="defaultValue"> is the default value to return when the document does not contain a value at <paramref name="field"/>.</param>
        /// <returns>The first value stored at the field in the DataDocument or <paramref name="defaultValue"/> if the field doesn't exist.</returns>
        public object GetFirstValue(string field, object defaultValue)
        {
            if(!dataMap.ContainsKey(field))
            {
                return defaultValue;
            }
            object currentValue = dataMap[field];
            if(currentValue is IEnumerable<object>)
            {
                return ((IEnumerable<object>)currentValue).First();
            } else
            {
                return currentValue;
            }            
        }

        /// <summary>
        /// This method retrieves the string representation of the first value in the DataDocument stored in the <paramref name="field"/> field.
        /// </summary>
        /// <param name="field"> is the field to retrieve the first value from.</param>
        /// <returns>The string representation of the first value stored at the field in the DataDocument or null if the field doesn't exist.</returns>
        public string GetFirstValueAsString(string field)
        {
            return this.GetFirstValueAsString(field, null);
        }

        /// <summary>
        /// This method retrieves the string representation of the first value in the DataDocument stored in the <paramref name="field"/> field.
        /// </summary>
        /// <param name="field"> is the field to retrieve the first value from.</param>
        /// <param name="defaultValue"> is the default value to return when the document does not contain a value at <paramref name="field"/>.</param>
        /// <returns>The string representation of the first value stored at the field in the DataDocument or <paramref name="defaultValue"/> if the field doesn't exist.</returns>
        public string GetFirstValueAsString(string field, string defaultValue) 
        {
            return this.GetFirstValue(field, defaultValue).ToString();
        }

        /// <summary>
        /// This method retrieves all of the values in the DataDocument stored in the <paramref name="field"/> field.
        /// </summary>
        /// <param name="field"> is the field to retrieves the values from.</param>
        /// <returns>The values stored at the field or an empty IEnumerable if the field doesn't exist.</returns>
        public IEnumerable<object> GetValues(string field)
        {
            if (!dataMap.ContainsKey(field))
            {
                return new List<object>();
            }
            object currentValue = dataMap[field];
            if (currentValue is IEnumerable<object>)
            {
                return ((IEnumerable<object>)currentValue);
            }
            else
            {
                List<object> result = new List<object>();
                result.Add(currentValue);
                return result;
            }
        }

        /// <summary>
        /// This method returns a string representation of the DataDocument. Is a UTF-8 encoded JSON string representation of the backing Dictionary.
        /// </summary>
        /// <returns>The string representation of DataDocument.</returns>
        public override String ToString()
        {
            return ToRedisValue();
        }

        /// <summary>
        /// This method converts the DataDocument into a <c>RedisValue</c> that can be persisted in Redis.
        /// </summary>
        /// <returns>A <c>RedisValue</c> representation of this DataDocument that can be persisted in Redis. </returns>
        internal RedisValue ToRedisValue()
        {
            return JsonSerializer.SerializeToUtf8Bytes<Dictionary<string, object>>(dataMap);
        }
    }    
}
