﻿using System;
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
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool IsDeleted { get; set; }
        public string Type => nameof(ApplicationUser);

        public virtual async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> userManager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            ClaimsIdentity userIdentity = await userManager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            this.AddClaims(userIdentity);

            // Add custom user claims here
            return userIdentity;
        }

        protected virtual ClaimsIdentity AddClaims(ClaimsIdentity identity)
        {
            return identity;
        }
    }
}
