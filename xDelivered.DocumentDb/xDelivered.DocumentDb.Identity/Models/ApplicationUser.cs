using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Identity.Models
{
    public abstract class ApplicationUser : IdentityUser, IDatabaseModelBase
    {
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Type { get; }
        public bool IsDeleted { get; set; }
    }
}
