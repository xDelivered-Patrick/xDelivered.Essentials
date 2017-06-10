using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web.Mvc;
using xDelivered.Common;
using ActionFilterAttribute = System.Web.Mvc.ActionFilterAttribute;

namespace xDelivered.Mvc
{
    /// <summary>
    ///     Secures MVC with Basic Auth
    /// 
    ///     important : set 'EnableBasicAuth' to true in web.config/appsettings to enable.
    /// 
    ///     For controller attribute settings, please use 'BasicAuthUsername' and 'BasicAuthPassword' in web.config/appsettings
    /// 
    /// </summary>
    public class BasicAuthenticationAttributeMvc : ActionFilterAttribute
    {
        public BasicAuthenticationAttributeMvc()
        {
            
        }

        public BasicAuthenticationAttributeMvc(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string BasicRealm { get; set; }
        protected string Username { get; set; }
        protected string Password { get; set; }
        public ActionResult Returns { get; set; } = new HttpStatusCodeResult(401);

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var isEnabled = ConfigurationManager.AppSettings["EnableBasicAuth"];
            if (isEnabled == null || !Convert.ToBoolean(isEnabled))
            {
                return;
            }

            //handle defaults
            if (Username.IsNullOrEmpty() && Password.IsNullOrEmpty())
            {
                Username = ConfigurationManager.AppSettings["BasicAuthUsername"];
                Password = ConfigurationManager.AppSettings["BasicAuthPassword"];
            }

            var req = filterContext.HttpContext.Request;
            var auth = req.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth))
            {
                var cred = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');
                var user = new { Name = cred[0], Pass = cred[1] };
                if (user.Name == Username && user.Pass == Password) return;
            }
            filterContext.HttpContext.Response.AddHeader("WWW-Authenticate", $"Basic realm=\"{BasicRealm ?? "REALM"}\"");
            filterContext.Result = Returns;
        }
    }
}
