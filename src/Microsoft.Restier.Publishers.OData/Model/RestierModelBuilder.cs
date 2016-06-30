// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Publishers.OData.Model
{
    /// <summary>
    /// This class will build the Edm model for OData service, user can extend this method to add more entity type
    /// or entity set or operation, then add it as Singleton DI service.
    /// </summary>
    public class RestierModelBuilder : IModelBuilder
    {
        /// <summary>
        /// Gets or sets namespace which will be used by all elements within the model.
        /// </summary>
        public string Namespace { get; set; }

        /// <inheritdoc/>
        public virtual Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            // Prepare for model build, now mainly for provider to collect information
            var modelPreparer = context.GetApiService<IModelPreparer>();
            if (modelPreparer != null)
            {
                modelPreparer.PrepareModelAsync(context, cancellationToken);
            }

<<<<<<< d164004a31b633cddb70cd798925235cabf4fcd4
            var entitySetTypeMap = context.ResourceSetTypeMap;
            if (entitySetTypeMap == null || entitySetTypeMap.Count == 0)
=======
            // Call some service to have provider set required information into ModelContext
            var builder = new ODataConventionModelBuilder();

            if (Namespace != null)
>>>>>>> Temp design, not ready for push yet
            {
                builder.Namespace = Namespace;
            }

            BuildEntityTypeEntitySetModel(context, builder);
            BuildOperationModel(context, builder);

            if (Namespace != null)
            {
                // reset entity type namespace which by default is clr class namespace
                foreach (var type in builder.StructuralTypes)
                {
                    type.Namespace = Namespace;
                }

                foreach (var type in builder.EnumTypes)
                {
                    type.Namespace = Namespace;
                }
            }

            // For ODataConversionModelBuilder, when auto binding navigation property,
            // navigation property CLR type can only have one related entity set,
            // or the binding will throw exception.
            // The code here will workaround this issue to binding to first entity set.
            foreach (var navigationSource in builder.NavigationSources)
            {
                builder.AddNavigationPropertyBinding(navigationSource);
            }

            return Task.FromResult(builder.GetEdmModel());
        }

        /// <summary>
        /// This can be override by sub class to add more entity type/entity set into the model via
        /// ODataConventionModelBuilder.
        /// </summary>
        /// <param name="context">The context for model builder</param>
        /// <param name="builder">The ODataConventionModelBuilder used to add more elements.</param>
        public virtual void BuildEntityTypeEntitySetModel(ModelContext context, ODataConventionModelBuilder builder)
        {
            if (context == null)
            {
                return;
            }

            var entitySetTypeMap = context.EntitySetTypeMap;
            if (entitySetTypeMap == null || entitySetTypeMap.Count == 0)
            {
                return;
            }

            foreach (var pair in entitySetTypeMap)
            {
                // Build a method with the specific type argument
                var specifiedMethod = EdmHelpers.EntitySetMethod.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                      pair.Key
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            entitySetTypeMap.Clear();

            var entityTypeKeyPropertiesMap = context.ResourceTypeKeyPropertiesMap;
            if (entityTypeKeyPropertiesMap != null)
            {
                foreach (var pair in entityTypeKeyPropertiesMap)
                {
                    var edmTypeConfiguration = builder.GetTypeConfigurationOrNull(pair.Key) as EntityTypeConfiguration;
                    if (edmTypeConfiguration == null)
                    {
                        continue;
                    }

                    foreach (var property in pair.Value)
                    {
                        edmTypeConfiguration.HasKey(property);
                    }
                }

                entityTypeKeyPropertiesMap.Clear();
            }
        }

        /// <summary>
        /// This can be override by sub class to add more operation into the model via ODataConventionModelBuilder.
        /// </summary>
        /// <param name="context">The context for model builder</param>
        /// <param name="builder">The ODataConventionModelBuilder used to add more operation.</param>
        public virtual void BuildOperationModel(ModelContext context, ODataConventionModelBuilder builder)
        {
            var apiType = context.GetApiService<ApiBase>().GetType();

            // Build entity set and singleton defined in Api class...
            var modelExtender = context.GetApiService<RestierModelExtender>();
            modelExtender.ScanForDeclaredPublicProperties();
            modelExtender.BuildEntitySetsAndSingletons(context, builder);

            // Build operation defined in Api class.
            var operationBuilder = new RestierOperationModelBuilder(apiType);
            operationBuilder.BuildModelForOperation(builder);
        }
    }
}
