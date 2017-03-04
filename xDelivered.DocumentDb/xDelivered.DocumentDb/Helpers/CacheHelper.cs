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
        public static string CreateKey<T>(string objectKey)
        {
            var t = typeof(T);
            if (t.DeclaringType != null)
            {
                t = t.DeclaringType;
            }

            if (objectKey.ToLower().Contains(t.Name.ToLower()))
            {
                //already has prefix
                return objectKey;
            }

            var keyPrefix = t.Name + ":".ToLower();
            
            StringBuilder builder = new StringBuilder();
            builder.Append(keyPrefix);

            builder.Append(objectKey);
            Debug.WriteLine(builder.ToString().ToLower());

            var r = builder.ToString().ToLower();
            if (r.Contains("mazfjqmeoe-qmjxys7mhrg"))
            {
                Debug.WriteLine("huh");
            }

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
