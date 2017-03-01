using System;

namespace xDelivered.Common
{
    public static class GuidEncoder
    {
        public static string Encode(string guidText)
        {
            Guid guid = new Guid(guidText);
            return Encode(guid);
        }

        public static string Encode(Guid guid)
        {
            string enc = Convert.ToBase64String(guid.ToByteArray());
            enc = enc.Replace("/", "-");
            enc = enc.Replace("+", "-");
            return enc.Substring(0, 22);
        }

        public static string CleanForTableStorage(string guidString)
        {
            guidString = guidString.Replace("_", string.Empty);
            guidString = guidString.Replace("-", string.Empty);

            return guidString.ToLower();
        }

        public static Guid Decode(string encoded)
        {
            encoded = encoded.Replace("_", "/");
            encoded = encoded.Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(encoded + "==");
            return new Guid(buffer);
        }
    }
}