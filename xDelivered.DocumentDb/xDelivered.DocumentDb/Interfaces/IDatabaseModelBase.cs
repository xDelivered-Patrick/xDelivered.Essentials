using System;
using Newtonsoft.Json;

namespace xDelivered.DocumentDb.Interfaces
{
    public interface IDatabaseModelBase
    {
        string id { get; set; }
        string Id { get; set; }
        DateTime Created { get; set; }
        DateTime? Updated { get; set; }
        string Type { get; }
        bool IsDeleted { get; set; }
    }
}