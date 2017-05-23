using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDelivered.Sampleapp.Helpers
{
    public class ConfigBase
    {
        public virtual string CosmosConnection { get; set; } = "https://localhost:8081";
        public virtual string RedisConnection { get; set; } = "localhost"; 
        public virtual string CosmosKey { get; set; } = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="; //emulator key
    }

    public class ProdConfig : ConfigBase
    {
        
    }
}
