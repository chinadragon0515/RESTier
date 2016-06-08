using System.Threading;
using Microsoft.Restier.Core.Query;

namespace Microsoft.OData.Service.Sample.Trippin.Extension
{
    public class CustomizedQueryExpressionAuthorizer : IQueryExpressionAuthorizer
    {
        public bool Authorize(QueryExpressionContext context)
        {
            var currentPrincipal = Thread.CurrentPrincipal;
            var userName = default(string);
            if (currentPrincipal != null)
            {
                userName = currentPrincipal.Identity.Name;
            }

            // TODO Some customized logic been done here

            return true;
        }
    }
}