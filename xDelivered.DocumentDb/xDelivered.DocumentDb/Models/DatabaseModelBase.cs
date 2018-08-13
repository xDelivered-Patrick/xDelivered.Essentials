using System;
using Newtonsoft.Json;
using xDelivered.Common;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Models
{
    /// <summary>
    /// Base of all documents
    /// </summary>
    public abstract class DatabaseModelBase : IDatabaseModelBase
    {
        public string id { get; set; } = Guid.NewGuid().ShortGuid().Replace("-", string.Empty).ToLower();

        public virtual string Id
        {
            get => id;
            set => id = value;
        }

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
