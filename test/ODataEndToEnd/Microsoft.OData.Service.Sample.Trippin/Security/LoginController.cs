using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace Microsoft.OData.Service.Sample.Trippin.Security
{
    /// <summary>
    /// This class is used to generate token after user is authentication.
    /// The token can be used for following request, and bearer token can be used here.
    /// This class is always not be part of restier odata service application but part of authentication server.
    /// </summary>
    [OverrideAuthentication]
    [AllowAnonymous]
    public class LoginController : ApiController
    {
        /// <summary>
        /// Authenticates user and returns token.
        /// </summary>
        /// <returns></returns>
        [OverrideAuthorization]
        [Route("login")]
        [HttpPost]
        public HttpResponseMessage Authenticate()
        {
            var authorizationHeader = this.Request.Headers.Authorization;

            // Get credential from the Authorization header (if present) and authenticate
            // Or get credential from request content
            bool isAuthenticated = true;

            // TODO Add customized authentication logic
            // Can use some existing authorization server here
            // This may validate based on username password or other supported credentials

            if (isAuthenticated)
            {
                var claims = new List<Claim>()
                    {
                    new Claim(ClaimTypes.Name, "test"),
                    new Claim(ClaimTypes.Role, "admin")
                    };
                var id = new ClaimsIdentity(claims, "Token");
                var principal = new ClaimsPrincipal(new[] { id });
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // TODO call generate token logic

            return null;
        }

        /// <summary>
        /// Returns token for the validated user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private HttpResponseMessage GetAuthToken(string userId)
        {
            // TODO Call some service to generate a token
            var token = "generatedToken";
            var response = Request.CreateResponse(HttpStatusCode.OK, "Authorized");
            response.Headers.Add("Token", token);

            // Some other headers
            return response;
        }
    }
}