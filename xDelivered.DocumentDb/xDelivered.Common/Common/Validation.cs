using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDelivered.Common.Common
{
    public static class Validate
    {
        public static void CheckForNull<T>(T obj, Func<T, object> lookup)
        {
            try
            {
                if (obj == null)
                {
                    throw new ArgumentException(nameof(obj));
                }

                CheckForNull(lookup(obj));
            }
            catch (Exception e)
            {
                throw new ArgumentException(nameof(obj));
            }
        }

        public static void CheckForNull(object value)
        {
            var s = value as string;
            if (s != null)
            {
                if (string.IsNullOrEmpty(s))
                {
                    throw new ArgumentException(nameof(value));
                }
            }
            else if (value == null)
            {
                throw new ArgumentException(nameof(value));
            }
        }

        public static void CheckCollectionAny(IList jobs)
        {
            if (jobs.Count == 0)
            {
                throw new ArgumentException(nameof(jobs));
            }
        }
    }
}
