using System;
using Newtonsoft.Json;

namespace xDelivered.DocumentDb.Interfaces
{
    public interface IDatabaseModelBase
    {
        [JsonProperty("id")]
        string Id { get; set; }
        DateTime Created { get; set; }
        DateTime? Updated { get; set; }
        string Type { get; }
        bool IsDeleted { get; set; }
    }
}