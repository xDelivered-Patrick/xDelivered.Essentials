using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using xDelivered.DocumentDb.Interfaces;
using xDelivered.DocumentDb.Models;

namespace xDelivered.DocumentDb.Identity.Models
{
    public class ApplicationUser : IdentityUser, IDatabaseModelBase
    {
        public new string Type => nameof(ApplicationUser);

        public virtual async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> userManager, string authType = DefaultAuthenticationTypes.ApplicationCookie)
        {
            ClaimsIdentity userIdentity = await userManager.CreateIdentityAsync(this, authType);

            // Add custom user claims here

            return userIdentity;
        }
    }
}
