// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.Publishers.OData.Model
{
    internal static class EdmHelpers
    {
        private static MethodInfo entitySetMethod = typeof (ODataConventionModelBuilder)
            .GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        private static MethodInfo singletonMethod = typeof (ODataConventionModelBuilder)
            .GetMethod("Singleton", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        private static MethodInfo collectionParameterMethod = typeof (ProcedureConfiguration)
            .GetMethod("CollectionParameter", BindingFlags.Public | BindingFlags.Instance);

        private static MethodInfo functionReturnsCollectionMethod = typeof (FunctionConfiguration)
            .GetMethod("ReturnsCollection", BindingFlags.Public | BindingFlags.Instance);

        private static MethodInfo actionReturnsCollectionMethod = typeof (ActionConfiguration)
            .GetMethod("ReturnsCollection", BindingFlags.Public | BindingFlags.Instance);

        private static MethodInfo functionReturnsFromEntitySetMethod = typeof (FunctionConfiguration)
            .GetMethod("ReturnsFromEntitySet", new[] {typeof (string)});

        private static MethodInfo actionReturnsFromEntitySetMethod = typeof (ActionConfiguration)
            .GetMethod("ReturnsFromEntitySet", new[] {typeof (string)});

        private static MethodInfo functionReturnsCollectionFromEntitySetMethod = typeof (FunctionConfiguration)
            .GetMethod("ReturnsCollectionFromEntitySet", new[] {typeof (string)});

        private static MethodInfo actionReturnsCollectionFromEntitySetMethod = typeof (ActionConfiguration)
            .GetMethod("ReturnsCollectionFromEntitySet", new[] {typeof (string)});

        public static MethodInfo EntitySetMethod
        {
            get { return entitySetMethod; }
        }

        public static MethodInfo SingletonMethod
        {
            get { return singletonMethod; }
        }

        public static MethodInfo CollectionParameterMethod
        {
            get { return collectionParameterMethod; }
        }

        public static MethodInfo FunctionReturnsCollectionMethod
        {
            get { return functionReturnsCollectionMethod; }
        }

        public static MethodInfo ActionReturnsCollectionMethod
        {
            get { return actionReturnsCollectionMethod; }
        }

        public static MethodInfo FunctionReturnsFromEntitySetMethod
        {
            get { return functionReturnsFromEntitySetMethod; }
        }

        public static MethodInfo ActionReturnsFromEntitySetMethod
        {
            get { return actionReturnsFromEntitySetMethod; }
        }

        public static MethodInfo FunctionReturnsCollectionFromEntitySetMethod
        {
            get { return functionReturnsCollectionFromEntitySetMethod; }
        }

        public static MethodInfo ActionReturnsCollectionFromEntitySetMethod
        {
            get { return actionReturnsCollectionFromEntitySetMethod; }
        }

        public static EdmTypeReference GetPrimitiveTypeReference(this Type type)
        {
            // Only handle primitive type right now
            bool isNullable;
            EdmPrimitiveTypeKind? primitiveTypeKind = EdmHelpers.GetPrimitiveTypeKind(type, out isNullable);

            if (!primitiveTypeKind.HasValue)
            {
                return null;
            }

            return new EdmPrimitiveTypeReference(
                EdmCoreModel.Instance.GetPrimitiveType(primitiveTypeKind.Value),
                isNullable);
        }

        private static EdmPrimitiveTypeKind? GetPrimitiveTypeKind(Type type, out bool isNullable)
        {
            isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
            if (isNullable)
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof (string))
            {
                return EdmPrimitiveTypeKind.String;
            }

            if (type == typeof (byte[]))
            {
                return EdmPrimitiveTypeKind.Binary;
            }

            if (type == typeof (bool))
            {
                return EdmPrimitiveTypeKind.Boolean;
            }

            if (type == typeof (byte))
            {
                return EdmPrimitiveTypeKind.Byte;
            }

            if (type == typeof (DateTime))
            {
                // TODO GitHubIssue#49 : how to map DateTime's in OData v4?  there is no Edm.DateTime type anymore
                return null;
            }

            if (type == typeof (DateTimeOffset))
            {
                return EdmPrimitiveTypeKind.DateTimeOffset;
            }

            if (type == typeof (decimal))
            {
                return EdmPrimitiveTypeKind.Decimal;
            }

            if (type == typeof (double))
            {
                return EdmPrimitiveTypeKind.Double;
            }

            if (type == typeof (Guid))
            {
                return EdmPrimitiveTypeKind.Guid;
            }

            if (type == typeof (short))
            {
                return EdmPrimitiveTypeKind.Int16;
            }

            if (type == typeof (int))
            {
                return EdmPrimitiveTypeKind.Int32;
            }

            if (type == typeof (long))
            {
                return EdmPrimitiveTypeKind.Int64;
            }

            if (type == typeof (sbyte))
            {
                return EdmPrimitiveTypeKind.SByte;
            }

            if (type == typeof (float))
            {
                return EdmPrimitiveTypeKind.Single;
            }

            if (type == typeof (TimeSpan))
            {
                // TODO GitHubIssue#49 : this should really be TimeOfDay,
                // but EdmPrimitiveTypeKind doesn't support that type.
                ////return EdmPrimitiveTypeKind.TimeOfDay;
                return EdmPrimitiveTypeKind.Duration;
            }

            if (type == typeof (void))
            {
                return null;
            }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture, Resources.NotSupportedType, type.FullName));
        }

        public static void AddNavigationPropertyBinding(this ODataConventionModelBuilder builder, INavigationSourceConfiguration configuration)
        {
            var entityTypeConfiguration = configuration.EntityType;
            var entityTypeCollection = new Collection<EntityTypeConfiguration>();

            // Add all super types
            var superType = entityTypeConfiguration.BaseType;
            while (superType != null)
            {
                entityTypeCollection.Add(superType);
                superType = superType.BaseType;
            }

            // Add all derived types
            var entityTypeConfigurations = entityTypeCollection.Concat(new[] { entityTypeConfiguration })
                .Concat(builder.DerivedTypes(entityTypeConfiguration));

            // For all types, add navigation properties binding
            foreach (var entityType in entityTypeConfigurations)
            {
                foreach (var navigationConfiguration in entityType.NavigationProperties)
                {
                    try
                    {
                        configuration.FindBinding(navigationConfiguration);
                    }
                    catch (NotSupportedException)
                    {
                        bool hasSingletonAttribute = navigationConfiguration.PropertyInfo.GetCustomAttributes<SingletonAttribute>().Any();
                        Type entityClrType = navigationConfiguration.RelatedClrType;

                        INavigationSourceConfiguration[] matchedNavigationSources;
                        if (hasSingletonAttribute)
                        {
                            matchedNavigationSources = builder.Singletons.Where(es => es.EntityType.ClrType == entityClrType).ToArray();
                        }
                        else
                        {
                            matchedNavigationSources = builder.EntitySets.Where(es => es.EntityType.ClrType == entityClrType).ToArray();
                        }

                        if (matchedNavigationSources.Length >= 1)
                        {
                            configuration.AddBinding(navigationConfiguration, matchedNavigationSources[0]);
                        }
                    }
                }
            }
        }

        public static IEnumerable<EntityTypeConfiguration> DerivedTypes(this ODataModelBuilder modelBuilder, EntityTypeConfiguration entity)
        {
            var derivedEntities = modelBuilder.StructuralTypes
                .OfType<EntityTypeConfiguration>().Where(e => e.BaseType == entity);

            foreach (EntityTypeConfiguration derivedType in derivedEntities)
            {
                yield return derivedType;
                foreach (var derivedDerivedType in modelBuilder.DerivedTypes(derivedType))
                {
                    yield return derivedDerivedType;
                }
            }
        }
    }
}