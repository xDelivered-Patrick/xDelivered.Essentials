using Newtonsoft.Json;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Models
{
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

        public T Resolve(IObjectResolver resolver)
        {
            if (Value != null) return Value;

            if (Link == null) return default(T);

            var v = resolver.Resolve<T>(Link);

            Value = v;

            return v;
        }

        public override string ToString()
        {
            return Link;
        }
    }
}