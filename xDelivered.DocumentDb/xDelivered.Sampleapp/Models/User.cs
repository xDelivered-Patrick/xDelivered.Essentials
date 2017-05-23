using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Models;

namespace xDelivered.Sampleapp.Models
{
    public class User : DatabaseModelBase
    {
        public override string Id => $"{FirstName} {LastName}";

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public int Age { get; set; }

        public List<ObjectLink<Tournament>> CompletedTournaments { get; set; } = new List<ObjectLink<Tournament>>();
    }
}
