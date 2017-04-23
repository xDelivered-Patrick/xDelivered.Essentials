using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xDelivered.Common
{
    public static class ManualResetEventSlimHelper
    {
        public static void BlockThreads(this ManualResetEventSlim source)
        {
            source.Reset();
        }

        public static void UnBlockThreads(this ManualResetEvent source)
        {
            source.Set();
        }
    }
}
