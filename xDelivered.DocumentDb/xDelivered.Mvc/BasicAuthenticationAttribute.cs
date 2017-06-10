using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

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

        public bool AllowMultiple => true;

        public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext,
            CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            var req = actionContext.Request;
            var auth = req.Headers.SingleOrDefault(x => x.Key == "Authorization");
            if (auth.Value != null && auth.Value.Any() && !string.IsNullOrEmpty(auth.Value.First()))
            {
                var cred = Encoding.ASCII
                    .GetString(Convert.FromBase64String(auth.Value.First().Substring(6))).Split(':');
                var user = new { Name = cred[0], Pass = cred[1] };
                if (user.Name == Username && user.Pass == Password) return continuation();
            }

            actionContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{BasicRealm ?? "REALM"}\"");
            return Task.FromResult(actionContext.Response);
        }
    }
}