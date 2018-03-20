using System;
using Newtonsoft.Json;
using xDelivered.Common;

namespace xDelivered.CosmosDb.Core
{
    public abstract class DatabaseModelBase : IDatabaseModelBase
    {
        [JsonProperty("id")]
        public virtual string Id { get; set; } = Guid.NewGuid().ShortGuid().Replace("-", string.Empty).ToLower();
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
        public string Type => this.GetType().Name;
        public bool IsDeleted { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
