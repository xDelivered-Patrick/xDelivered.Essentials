using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using xDelivered.Common;
using xDelivered.Common.Common;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Models
{
    public abstract class DatabaseModelBase : IDatabaseModelBase
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
        public string Type => this.GetType().Name;
        public bool IsDeleted { get; set; }

        protected DatabaseModelBase()
        {
            SetId();
        }

        protected virtual void SetId()
        {
            Id = Guid.NewGuid().ShortGuid().Replace("-", string.Empty).ToLower();
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
