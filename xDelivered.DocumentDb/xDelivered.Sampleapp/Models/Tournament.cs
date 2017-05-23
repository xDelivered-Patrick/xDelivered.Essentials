using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Models;

namespace xDelivered.Sampleapp.Models
{
    public class Tournament : DatabaseModelBase
    {
        public string Name { get; set; }
        public DateTime Starts { get; set; }

        public ObjectLink<User> Owner { get; set; } 
        public ObjectLink<Sport> Sport { get; set; }

        public List<ObjectLink<User>> Users { get; set; } = new List<ObjectLink<User>>();
    }
}
