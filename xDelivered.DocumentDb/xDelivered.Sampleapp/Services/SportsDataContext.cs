using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Services;
using xDelivered.Sampleapp.Helpers;
using xDelivered.Sampleapp.Models;

namespace xDelivered.Sampleapp.Services
{
    public class SportsDataContext : XDbProvider
    {
        public SportsDataContext(ConfigBase config) : base(config.RedisConnection, new CosmosProvider(config.CosmosConnection, config.CosmosKey, "main"))
        {
            
        }

        public List<Tournament> ThisWeeksTournaments()
        {
            var seven7dayAhead = DateTime.UtcNow.AddDays(7);

            return base.DocumentCosmosDb.NewQuery<Tournament>()
                .Where(x => x.Type == nameof(Tournament) && x.Starts < seven7dayAhead)
                .ToList();
        }
    }
}
