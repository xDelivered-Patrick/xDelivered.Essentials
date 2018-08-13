using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using xDelivered.Common;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Identity.Models
{
    public class IdentityUser : IUser, IDatabaseModelBase
    {
        private string _userName;


        public string id { get; set; } = Guid.NewGuid().ShortGuid().Replace("-", string.Empty).ToLower();

        public virtual string Id
        {
            get => id;
            set => id = value;
        }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Type { get; }
        public bool IsDeleted { get; set; }

        public string UserName
        {
            get { return _userName.ToLower(); }
            set { _userName = value; }
        }

        /// <summary>
        ///     Email
        /// </summary>
        public virtual string Email { get; set; }

        /// <summary>
        ///     True if the email is confirmed, default is false
        /// </summary>
        public virtual bool EmailConfirmed { get; set; }

        /// <summary>
        ///     The salted/hashed form of the user password
        /// </summary>
        public virtual string PasswordHash { get; set; }

        /// <summary>
        /// A random value that should change whenever a users credentials change (password changed, login removed)
        /// </summary>
        public virtual string SecurityStamp { get; set; }

        /// <summary>
        ///     PhoneNumber for the user
        /// </summary>
        public virtual string PhoneNumber { get; set; }

        /// <summary>
        ///     True if the phone number is confirmed, default is false
        /// </summary>
        public virtual bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        ///     Is two factor enabled for the user
        /// </summary>
        public virtual bool TwoFactorEnabled { get; set; }

        /// <summary>
        ///     DateTime in UTC when lockout ends, any time in the past is considered not locked out.
        /// </summary>
        public virtual DateTimeOffset LockoutEnd { get; set; }

        /// <summary>
        ///     Is lockout enabled for this user
        /// </summary>
        public virtual bool LockoutEnabled { get; set; }

        /// <summary>
        ///     Used to record failures for the purposes of lockout
        /// </summary>
        public virtual int AccessFailedCount { get; set; }

        /// <summary>
        /// Gets the logins.
        /// </summary>
        /// <value>The logins.</value>
        public virtual List<UserLoginInfo> Logins { get; private set; }

        /// <summary>
        /// Gets the claims.
        /// </summary>
        /// <value>The claims.</value>
        public virtual List<IdentityUserClaim> Claims { get; private set; }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <value>The roles.</value>
        public virtual List<string> Roles { get; private set; }

        public IdentityUser()
        {
            Claims = new List<IdentityUserClaim>();
            Roles = new List<string>();
            Logins = new List<UserLoginInfo>();
        }

        public IdentityUser(string userName) : this()
        {
            UserName = userName;
        }
    }
}