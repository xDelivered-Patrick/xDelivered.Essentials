using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using ActionFilterAttribute = System.Web.Mvc.ActionFilterAttribute;

namespace xDelivered.Mvc
{
    /// <summary>
    ///     Secures MVC with Basic Auth
    /// </summary>
    public class BasicAuthenticationAttributeMvc : ActionFilterAttribute
    {
        public BasicAuthenticationAttributeMvc(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string BasicRealm { get; set; }
        protected string Username { get; set; }
        protected string Password { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var req = filterContext.HttpContext.Request;
            var auth = req.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth))
            {
                var cred = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');
                var user = new { Name = cred[0], Pass = cred[1] };
                if (user.Name == Username && user.Pass == Password) return;
            }
            filterContext.HttpContext.Response.AddHeader("WWW-Authenticate", $"Basic realm=\"{BasicRealm ?? "REALM"}\"");
            filterContext.Result = new HttpUnauthorizedResult();
        }
    }
}
