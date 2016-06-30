// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Publishers.OData.Model
{
    /// <summary>
    /// A convention-based API model builder that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    internal class RestierModelExtender
    {
        private readonly Type targetType;
        private readonly ICollection<PropertyInfo> publicProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> entitySetProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> singletonProperties = new List<PropertyInfo>();

        internal RestierModelExtender(Type targetType)
        {
            this.targetType = targetType;
        }

        public static void ApplyTo(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");

            // The model builder must maintain a singleton life time, for holding states and being injected into
            // some other services.
            services.AddSingleton(new RestierModelExtender(targetType));

            services.AddService<IModelMapper, ModelMapper>();
            services.AddService<IQueryExpressionExpander, QueryExpressionExpander>();
            services.AddService<IQueryExpressionSourcer, QueryExpressionSourcer>();
        }

        internal void ScanForDeclaredPublicProperties()
        {
            var currentType = this.targetType;
            while (currentType != null && currentType != typeof(ApiBase))
            {
                var publicPropertiesDeclaredOnCurrentType = currentType.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

                foreach (var property in publicPropertiesDeclaredOnCurrentType)
                {
                    if (property.CanRead &&
                        publicProperties.All(p => p.Name != property.Name))
                    {
                        publicProperties.Add(property);
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        internal void BuildEntitySetsAndSingletons(ModelContext context, ODataConventionModelBuilder builder)
        {
            var configuration = context.ApiContext.Configuration;
            foreach (var property in this.publicProperties)
            {
                if (configuration.IsPropertyIgnored(property.Name))
                {
                    continue;
                }

                var isEntitySet = IsEntitySetProperty(property);
                if (!isEntitySet)
                {
                    if (!IsSingletonProperty(property))
                    {
                        continue;
                    }
                }

                var propertyType = property.PropertyType;
                if (isEntitySet)
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                var typeConfiguration = builder.GetTypeConfigurationOrNull(propertyType);
                if (typeConfiguration == null)
                {
                    // The type must be added before singleton or entity set
                    // Property like DbContext, ApiContext will be ignored here
                    continue;
                }

                if (isEntitySet)
                {
                    var specifiedMethod = EdmHelpers.EntitySetMethod.MakeGenericMethod(propertyType);
                    var parameters = new object[]
                    {
                            property.Name
                    };

                    specifiedMethod.Invoke(builder, parameters);
                }
                else
                {
                    var specifiedMethod = EdmHelpers.SingletonMethod.MakeGenericMethod(propertyType);
                    var parameters = new object[]
                    {
                            property.Name
                    };

                    specifiedMethod.Invoke(builder, parameters);
                }
            }
        }

        private static bool IsEntitySetProperty(PropertyInfo property)
        {
            return property.PropertyType.IsGenericType &&
                   property.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                   property.PropertyType.GetGenericArguments()[0].IsClass;
        }

        private static bool IsSingletonProperty(PropertyInfo property)
        {
            return !property.PropertyType.IsGenericType && property.PropertyType.IsClass;
        }

        private IQueryable GetEntitySetQuery(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");
            if (context.ModelReference == null)
            {
                return null;
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubModelReference;
            if (dataSourceStubReference == null)
            {
                return null;
            }

            var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }

            var entitySetProperty = this.entitySetProperties
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty != null)
            {
                object target = null;
                if (!entitySetProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.GetApiService<ApiBase>();
                    if (target == null ||
                        !this.targetType.IsInstanceOfType(target))
                    {
                        return null;
                    }
                }

                return entitySetProperty.GetValue(target) as IQueryable;
            }

            return null;
        }

        private IQueryable GetSingletonQuery(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");
            if (context.ModelReference == null)
            {
                return null;
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubModelReference;
            if (dataSourceStubReference == null)
            {
                return null;
            }

            var singleton = dataSourceStubReference.Element as IEdmSingleton;
            if (singleton == null)
            {
                return null;
            }

            var singletonProperty = this.singletonProperties
                .SingleOrDefault(p => p.Name == singleton.Name);
            if (singletonProperty != null)
            {
                object target = null;
                if (!singletonProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.GetApiService<ApiBase>();
                    if (target == null ||
                        !this.targetType.IsInstanceOfType(target))
                    {
                        return null;
                    }
                }

                var value = Array.CreateInstance(singletonProperty.PropertyType, 1);
                value.SetValue(singletonProperty.GetValue(target), 0);
                return value.AsQueryable();
            }

            return null;
        }

        internal class ModelMapper : IModelMapper
        {
            public ModelMapper(RestierModelExtender modelCache)
            {
                ModelCache = modelCache;
            }

            public RestierModelExtender ModelCache { get; set; }

            private IModelMapper InnerModelMapper { get; set; }

            /// <inheritdoc/>
            public bool TryGetRelevantType(
                ApiContext context,
                string name,
                out Type relevantType)
            {
                if (this.InnerModelMapper != null &&
                    this.InnerModelMapper.TryGetRelevantType(context, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                var entitySetProperty = this.ModelCache.entitySetProperties.SingleOrDefault(p => p.Name == name);
                if (entitySetProperty != null)
                {
                    relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
                }

                if (relevantType == null)
                {
                    var singletonProperty = this.ModelCache.singletonProperties.SingleOrDefault(p => p.Name == name);
                    if (singletonProperty != null)
                    {
                        relevantType = singletonProperty.PropertyType;
                    }
                }

                return relevantType != null;
            }

            /// <inheritdoc/>
            public bool TryGetRelevantType(
                ApiContext context,
                string namespaceName,
                string name,
                out Type relevantType)
            {
                if (this.InnerModelMapper != null &&
                    this.InnerModelMapper.TryGetRelevantType(context, namespaceName, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                return false;
            }
        }

        internal class QueryExpressionExpander : IQueryExpressionExpander
        {
            public QueryExpressionExpander(RestierModelExtender modelCache)
            {
                ModelCache = modelCache;
            }

            /// <inheritdoc/>
            public IQueryExpressionExpander InnerHandler { get; set; }

            private RestierModelExtender ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression Expand(QueryExpressionContext context)
            {
                Ensure.NotNull(context, "context");

                var result = CallInner(context);
                if (result != null)
                {
                    return result;
                }

                // Ensure this query constructs from DataSourceStub.
                if (context.ModelReference is DataSourceStubModelReference)
                {
                    // Only expand entity set query which returns IQueryable<T>.
                    var query = ModelCache.GetEntitySetQuery(context);
                    if (query != null)
                    {
                        return query.Expression;
                    }
                }

                // No expansion happened just return the node itself.
                return context.VisitedNode;
            }

            private Expression CallInner(QueryExpressionContext context)
            {
                if (this.InnerHandler != null)
                {
                    return this.InnerHandler.Expand(context);
                }

                return null;
            }
        }

        internal class QueryExpressionSourcer : IQueryExpressionSourcer
        {
            public QueryExpressionSourcer(RestierModelExtender modelCache)
            {
                ModelCache = modelCache;
            }

            public IQueryExpressionSourcer InnerHandler { get; set; }

            private RestierModelExtender ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                var result = CallInner(context, embedded);
                if (result != null)
                {
                    // Call the provider's sourcer to find the source of the query.
                    return result;
                }

                // This sourcer ONLY deals with queries that cannot be addressed by the provider
                // such as a singleton query that cannot be sourced by the EF provider, etc.
                var query = ModelCache.GetEntitySetQuery(context) ?? ModelCache.GetSingletonQuery(context);
                if (query != null)
                {
                    return Expression.Constant(query);
                }

                return null;
            }

            private Expression CallInner(QueryExpressionContext context, bool embedded)
            {
                if (this.InnerHandler != null)
                {
                    return this.InnerHandler.ReplaceQueryableSource(context, embedded);
                }

                return null;
            }
        }
    }
}
