using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDelivered.DocumentDb.Helpers
{
    public static class CacheHelper
    {
        public static string CreateKey<T>(string objectKey)
        {
            var keyPrefix = typeof(T).Name + ":";

            if (objectKey.Contains(keyPrefix))
            {
                //already has prefix
                return objectKey;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(typeof(T).Name + ":");

            builder.Append(objectKey);

            return builder.ToString();
        }
    }
}
