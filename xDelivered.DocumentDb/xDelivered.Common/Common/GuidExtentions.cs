using System;

namespace xDelivered.Common.Common
{
    public static class GuidExtentions
    {
        public static string ShortGuid(this Guid guid)
        {
            return GuidEncoder.Encode(guid);
        }
    }
}
