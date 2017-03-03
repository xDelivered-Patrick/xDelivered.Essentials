using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Helpers
{
    public static class CacheHelper
    {
        public static string CreateKey<T>(string objectKey)
        {
            var keyPrefix = typeof(T).Name.ToLower() + ":";

            if (objectKey.Contains(keyPrefix))
            {
                //already has prefix
                return objectKey;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(typeof(T).Name + ":");

            builder.Append(objectKey);

            System.Diagnostics.Debug.WriteLine(builder.ToString());
            return builder.ToString().ToLower();
        }

        public static string CreateKey<T>(T applicationUser, Func<T, string> idLookup)
        {
            string keyPrefix = applicationUser.GetType().Name + ":";
            string id = idLookup.Invoke(applicationUser);
            
            StringBuilder builder = new StringBuilder();
            builder.Append(keyPrefix);
            builder.Append(id);
            return builder.ToString().ToLower();
        }
    }
}
