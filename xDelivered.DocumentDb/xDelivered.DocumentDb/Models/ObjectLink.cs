using Newtonsoft.Json;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Services;

namespace xDelivered.DocumentDb.Models
{
    /// <summary>
    /// Comprises a loose relationship between two Documents allowing relationships to be constructed in a NoSQL manner.
    /// 
    /// Will hold value in memory. If no object then pulls from either Redis or CosmosDb
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectLink<T> where T : IDatabaseModelBase
    {
        public ObjectLink() { }
        public ObjectLink(string id, string identifier = null)
        {
            Link = id;
            Identifier = identifier;
        }

        public ObjectLink(T obj)
        {
            Value = obj;
            Link = StoreLinkValue(obj);
            Identifier = obj.ToString();
        }

        private static string StoreLinkValue(T obj)
        {
            var name = typeof(T).Name;
            return obj.Id.Replace($"{name}-", string.Empty);
        }

        public string Link { get; set; }
        public string Identifier { get; set; }

        [JsonIgnore]
        public T Value { get; set; }

        [JsonIgnore]
        public bool HasLink => !string.IsNullOrEmpty(Link);

        /// <summary>
        /// Pulls object from link. Order of attempts : 1. Memory 2. Redis 3. CosmosDb 
        /// </summary>
        /// <returns>Document</returns>
        public T Resolve(IObjectResolver resolver = null)
        {
            if (Value != null) return Value;

            if (Link == null) return default(T);

            var rResolver = resolver ?? XDbProvider.Resolver;

            var v = rResolver.Resolve<T>(Link);

            Value = v;

            return v;
        }

        public override string ToString()
        {
            return Link;
        }
    }
}