using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using xDelivered.Common;
using IActionFilter = System.Web.Http.Filters.IActionFilter;

namespace xDelivered.Mvc
{
    /// <summary>
    ///     Secures WebApi with Basic Auth
    /// </summary>
    public class BasicAuthenticationAttribute : IActionFilter
    {
        public BasicAuthenticationAttribute(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string BasicRealm { get; set; }
        protected string Username { get; set; }
        protected string Password { get; set; }
        protected HttpStatusCode Returns { get; set; } = HttpStatusCode.Forbidden;

        public bool AllowMultiple => true;

        public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext,
            CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            //handle defaults
            if (Username.IsNullOrEmpty() && Password.IsNullOrEmpty())
            {
                Username = ConfigurationManager.AppSettings["BasicAuthUsername"];
                Password = ConfigurationManager.AppSettings["BasicAuthPassword"];
            }

            var req = actionContext.Request;
            var auth = req.Headers.SingleOrDefault(x => x.Key == "Authorization");
            if (auth.Value != null && auth.Value.Any() && !string.IsNullOrEmpty(auth.Value.First()))
            {
                var cred = Encoding.ASCII
                    .GetString(Convert.FromBase64String(auth.Value.First().Substring(6))).Split(':');
                var user = new { Name = cred[0], Pass = cred[1] };
                if (user.Name == Username && user.Pass == Password) return continuation();
            }

            actionContext.Response = new HttpResponseMessage(Returns);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{BasicRealm ?? "REALM"}\"");
            return Task.FromResult(actionContext.Response);
        }
    }
}