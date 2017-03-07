using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace xDelivered.Common
{
    public static class TimespanExtentions
    {
        public static string ToIsoFormat(this TimeSpan source)
        {
            return XmlConvert.ToString(source);
        }
    }
}
