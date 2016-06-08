using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Microsoft.OData.Service.Sample.Trippin.Security
{
    /// <summary>
    /// This class is optional, user can use Authorize attribute directly to validate the roles.
    /// Also user can add any customization logic here for authorization. 
    /// </summary>
    public class CustomizedAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext context)
        {
            // Get principal from header
            var principal = context.Request.GetRequestContext().Principal;

            // TODO Validate whether the user isAuthorized

            return true;
        }
    }
}