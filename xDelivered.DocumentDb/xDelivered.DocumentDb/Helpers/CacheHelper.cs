using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Helpers
{
    public static class CacheHelper
    {
        /// <summary>
        /// Creates a key based from a documents name. Will prefix the documents type to the ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectKey"></param>
        /// <returns></returns>
        public static string CreateKey<T>(string objectKey)
        {
            var keyPrefix = $"{typeof(T).Name.ToLower()}:";

            if (objectKey.ToLower().Contains($"{typeof(T).Name.ToLower()}:"))
            {
                //already has prefix
                return objectKey;
            }
            
            StringBuilder builder = new StringBuilder();
            builder.Append(keyPrefix);
            builder.Append(objectKey);
            return builder.ToString().ToLower();
        }

        /// <summary>
        /// Creates a key based from a documents name. Will prefix the documents type to the ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string CreateKey<T>(T @object, Func<T, string> idLookup)
        {
            string keyPrefix = $"{@object.GetType().Name}:";
            string id = idLookup.Invoke(@object);
            
            if (id.ToLower().Contains($"{typeof(T).Name.ToLower()}:"))
            {
                //already has prefix
                return id.ToLower();
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(keyPrefix);
            builder.Append(id);
            return builder.ToString().ToLower();
        }

        public static string RemoveKeyPrefixes(string key)
        {
            var last = key.LastIndexOf(":", StringComparison.Ordinal);
            if (last != -1)
            {
                key = key.Remove(0,last + 1);
            }

            return key;
        }
    }
}
