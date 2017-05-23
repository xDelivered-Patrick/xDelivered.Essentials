using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Models;

namespace xDelivered.Sampleapp.Models
{
    public class Sport : DatabaseModelBase
    {
        public string Name { get; set; }

        public List<User> Users { get; set; } = new List<User>();
    }
}
