﻿using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Publisher.OData.Model
{
    class RestierModelBuilder : IModelBuilder
    {
        public IModelBuilder InnerModelBuilder { get; set; }
        
        /// <inheritdoc/>
        public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            // This means user build a model with customized model builder registered as inner most,
            // no logic will be done here
            if (InnerModelBuilder != null)
            {
                var innerModel = await InnerModelBuilder.GetModelAsync(context, cancellationToken);
                if (innerModel != null)
                {
                    return innerModel;
                }
            }

            var collection = context.EntitySetTypeMapCollection;
            if (collection == null || collection.Count == 0)
            {
                return null;
            }

            // Collection is set by EF now, and EF model producer will not build model any more
            var builder = new ODataConventionModelBuilder();
            MethodInfo method = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in collection)
            {
                //Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                      pair.Key
                };
                
                specifiedMethod.Invoke(builder, parameters);
            }

            context.EntitySetTypeMapCollection.Clear();

            return builder.GetEdmModel();
        }
    }
}