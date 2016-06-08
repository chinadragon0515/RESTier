using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace Microsoft.OData.Service.Sample.Trippin.Security
{
    /// <summary>
    /// This class is used to authenticated the request with principal like bearer token from authentication system.
    /// It will set current user name and role into context for following authorization. 
    /// </summary>
    public class CustomizedAuthenticationFilter : Attribute, IAuthenticationFilter
    {
        public bool AllowMultiple { get; }

        public Task AuthenticateAsync(HttpAuthenticationContext context,CancellationToken cancellationToken)
        {
            var authorizationHeader = context.Request.Headers.Authorization;
            // Get credential from the Authorization header 

            bool isAuthenticated = true;

            // TODO Add customized authentication logic
            // This may authenticate with token which is retrieved from login request

            if (isAuthenticated)
            {
                var claims = new List<Claim>()
                    {
                    new Claim(ClaimTypes.Name, "test"),
                    new Claim(ClaimTypes.Role, "admin")
                    };
                var id = new ClaimsIdentity(claims, "Token");
                var principal = new ClaimsPrincipal(new[] { id });

                // Set to context and Current.User so it can be got with Thread.CurrentPrincipal
                context.Principal = principal;
                HttpContext.Current.User = principal;
            }
            else
            {
                context.ErrorResult = new UnauthorizedResult(
                  new AuthenticationHeaderValue[0], context.Request);
            }

            return Task.FromResult(0);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            var challenge = new AuthenticationHeaderValue("Basic");
            context.Result = new AddChallengeOnUnauthorizedResult(challenge, context.Result);
            return Task.FromResult(0);
        }

        public class AddChallengeOnUnauthorizedResult : IHttpActionResult
        {
            public AddChallengeOnUnauthorizedResult(AuthenticationHeaderValue challenge, IHttpActionResult innerResult)
            {
                Challenge = challenge;
                InnerResult = innerResult;
            }

            public AuthenticationHeaderValue Challenge { get; private set; }

            public IHttpActionResult InnerResult { get; private set; }

            public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                HttpResponseMessage response = await InnerResult.ExecuteAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Only add one challenge per authentication scheme.
                    if (!response.Headers.WwwAuthenticate.Any((h) => h.Scheme == Challenge.Scheme))
                    {
                        response.Headers.WwwAuthenticate.Add(Challenge);
                    }
                }

                return response;
            }
        }
    }
}