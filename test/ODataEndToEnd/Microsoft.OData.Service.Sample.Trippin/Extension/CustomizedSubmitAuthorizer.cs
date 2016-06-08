using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.OData.Service.Sample.Trippin.Extension
{
    public class CustomizedSubmitAuthorizer : IChangeSetItemAuthorizer
    {
        private IChangeSetItemProcessor InnerAuthorizer { get; set; }

        public Task<bool> AuthorizeAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            var currentPrincipal = Thread.CurrentPrincipal;
            var userName = default(string);
            if (currentPrincipal != null)
            {
                userName = currentPrincipal.Identity.Name;
            }

            // TODO Some customized logic been done here

            // If can<> related method defined in Api need to be called, call InnerAuthorizer.AuthorizeAsync

            return Task.FromResult(true);
        }
    }
}