// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Expressions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Publishers.OData.Model
{
    internal class RestierOperationModelBuilder
    {
        private readonly Type targetType;
        private readonly ICollection<OperationMethodInfo> operationInfos = new List<OperationMethodInfo>();

        internal RestierOperationModelBuilder(Type targetType)
        {
            this.targetType = targetType;
        }

        public void BuildModelForOperation(ODataConventionModelBuilder builder)
        {
            this.ScanForOperations();
            this.BuildOperations(builder);
        }

        private static void AddParameters(ProcedureConfiguration operationConfiguration, OperationMethodInfo methodInfo)
        {
            // TODO ProcedureConfiguration is changed to OperationConfiguration in Web Api OData 6.x
            var parameters = methodInfo.Method.GetParameters();
            int index = 0;
            if (methodInfo.IsBound)
            {
                index = 1;
            }

            for (; index < parameters.Length; index++)
            {
                var parameterType = parameters[index].ParameterType;

                var genericType = parameterType.FindGenericType(typeof(IEnumerable<>));
                if (genericType == null)
                {
                    operationConfiguration.Parameter(parameters[index].ParameterType, parameters[index].Name);
                }
                else
                {
                    // Collection parameters need to use CollectionParameter method to add
                    var elementType = genericType.GenericTypeArguments[0];
                    var method = EdmHelpers.CollectionParameterMethod.MakeGenericMethod(elementType);
                    method.Invoke(operationConfiguration, new object[] { parameters[index].Name });
                }
            }

            if (methodInfo.Namespace != null)
            {
                operationConfiguration.Namespace = methodInfo.Namespace;
            }
        }

        private static void AddFunctionReturn(
            FunctionConfiguration functionConfiguration,
            Type returnType,
            ODataConventionModelBuilder builder,
            string entitysetName)
        {
            if (returnType == typeof(void))
            {
                return;
            }

            var genericType = returnType.FindGenericType(typeof(IEnumerable<>));
            if (genericType == null)
            {
                var returnTypeConfiguration = builder.GetTypeConfigurationOrNull(returnType);
                if (returnTypeConfiguration == null || !(returnTypeConfiguration is EntityTypeConfiguration))
                {
                    // returns method is not in class OperationConfiguration
                    functionConfiguration.Returns(returnType);
                    return;
                }

                entitysetName = FindEntitySetName(builder, returnType, entitysetName);
                if (entitysetName == null)
                {
                    // Will not add the return value here
                    return;
                }

                var method = EdmHelpers.FunctionReturnsFromEntitySetMethod.MakeGenericMethod(returnType);
                method.Invoke(functionConfiguration, new[] { entitysetName });
            }
            else
            {
                // Collection returns need to use ReturnsCollection method
                var elementType = genericType.GenericTypeArguments[0];

                var returnTypeConfiguration = builder.GetTypeConfigurationOrNull(elementType);
                if (returnTypeConfiguration == null || !(returnTypeConfiguration is EntityTypeConfiguration))
                {
                    var method = EdmHelpers.FunctionReturnsCollectionMethod.MakeGenericMethod(elementType);
                    method.Invoke(functionConfiguration, null);
                    return;
                }

                entitysetName = FindEntitySetName(builder, elementType, entitysetName);
                if (entitysetName == null)
                {
                    // Will not add the return value here
                    return;
                }

                var method2 = EdmHelpers.FunctionReturnsCollectionFromEntitySetMethod.MakeGenericMethod(elementType);
                method2.Invoke(functionConfiguration, new object[] { entitysetName });
            }
        }

        private static void AddActionReturn(
            ActionConfiguration actionConfiguration,
            Type returnType,
            ODataConventionModelBuilder builder,
            string entitysetName)
        {
            if (returnType == typeof(void))
            {
                return;
            }

            var genericType = returnType.FindGenericType(typeof(IEnumerable<>));
            if (genericType == null)
            {
                var returnTypeConfiguration = builder.GetTypeConfigurationOrNull(returnType);
                if (returnTypeConfiguration == null || !(returnTypeConfiguration is EntityTypeConfiguration))
                {
                    // returns method is not in class OperationConfiguration
                    actionConfiguration.Returns(returnType);
                    return;
                }

                entitysetName = FindEntitySetName(builder, returnType, entitysetName);
                if (entitysetName == null)
                {
                    // Will not add the return value here
                    return;
                }

                var method = EdmHelpers.ActionReturnsFromEntitySetMethod.MakeGenericMethod(returnType);
                method.Invoke(actionConfiguration, new object[] { entitysetName });
            }
            else
            {
                // Collection returns need to use ReturnsCollection method
                var elementType = genericType.GenericTypeArguments[0];

                var returnTypeConfiguration = builder.GetTypeConfigurationOrNull(elementType);
                if (returnTypeConfiguration == null || !(returnTypeConfiguration is EntityTypeConfiguration))
                {
                    var method = EdmHelpers.ActionReturnsCollectionMethod.MakeGenericMethod(elementType);
                    method.Invoke(actionConfiguration, null);
                    return;
                }

                entitysetName = FindEntitySetName(builder, elementType, entitysetName);
                if (entitysetName == null)
                {
                    // Will not add the return value here
                    return;
                }

                var method2 = EdmHelpers.ActionReturnsCollectionFromEntitySetMethod.MakeGenericMethod(elementType);
                method2.Invoke(actionConfiguration, new object[] { entitysetName });
            }
        }

        private static string FindEntitySetName(
            ODataConventionModelBuilder builder, Type returnType, string entitysetName)
        {
            if (entitysetName != null)
            {
                return entitysetName;
            }

            // Need to find entity set name
            foreach (var entitySet in builder.EntitySets)
            {
                if (entitySet.ClrType == returnType)
                {
                    entitysetName = entitySet.Name;
                    break;
                }
            }

            return entitysetName;
        }

        private void ScanForOperations()
        {
            var methods = this.targetType.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

            foreach (var method in methods)
            {
                var operationAttribute = method.GetCustomAttributes<OperationAttribute>(true).FirstOrDefault();
                if (operationAttribute != null)
                {
                    operationInfos.Add(new OperationMethodInfo
                    {
                        Method = method,
                        OperationAttribute = operationAttribute
                    });
                }
            }
        }

        private void BuildOperations(ODataConventionModelBuilder builder)
        {
            foreach (OperationMethodInfo operationMethodInfo in this.operationInfos)
            {
                bool isBound = operationMethodInfo.IsBound;
                var bindingParameter = operationMethodInfo.Method.GetParameters().FirstOrDefault();

                if (bindingParameter == null && isBound)
                {
                    // Ignore the method which is marked as bounded but no parameters
                    continue;
                }

                var returnType = operationMethodInfo.Method.ReturnType;

                if (!isBound && !operationMethodInfo.HasSideEffects)
                {
                    // Unbound function
                    var functionConfiguration = builder.Function(operationMethodInfo.Name);
                    if (operationMethodInfo.IsComposable)
                    {
                        functionConfiguration.IsComposable = true;
                    }

                    AddParameters(functionConfiguration, operationMethodInfo);
                    AddFunctionReturn(functionConfiguration, returnType, builder, operationMethodInfo.EntitySet);
                }
                else if (!isBound && operationMethodInfo.HasSideEffects)
                {
                    // Unbound action
                    var actionConfiguration = builder.Action(operationMethodInfo.Name);
                    AddParameters(actionConfiguration, operationMethodInfo);
                    AddActionReturn(actionConfiguration, returnType, builder, operationMethodInfo.EntitySet);
                }
                else
                {
                    // Bound operation
                    var bindingType = bindingParameter.ParameterType;
                    var boundCollection = false;
                    var genericType = bindingType.FindGenericType(typeof(IEnumerable<>));
                    if (genericType != null)
                    {
                        boundCollection = true;
                        bindingType = genericType.GenericTypeArguments[0];
                    }

                    var typeConfiguration = builder.GetTypeConfigurationOrNull(bindingType);
                    var entityTypeConfiguration = typeConfiguration as EntityTypeConfiguration;
                    if (entityTypeConfiguration == null)
                    {
                        // OData conversion module builder does not support operation bound to non-entity type now
                        continue;
                    }

                    var configGenericType = typeof(EntityTypeConfiguration<>).MakeGenericType(bindingType);
                    var constructor = configGenericType.GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(ODataModelBuilder), typeof(EntityTypeConfiguration) },
                        null);
                    var genericEntityTypeConfiguration =
                        constructor.Invoke(new object[] { builder, entityTypeConfiguration });

                    var typeContainedMethod = configGenericType;
                    var instanceCallMethod = genericEntityTypeConfiguration;
                    if (boundCollection)
                    {
                        instanceCallMethod =
                            configGenericType.GetProperty("Collection").GetValue(genericEntityTypeConfiguration, null);
                        typeContainedMethod = typeof(EntityCollectionConfiguration<>).MakeGenericType(bindingType);
                    }

                    if (!operationMethodInfo.HasSideEffects)
                    {
                        var functionMethod = typeContainedMethod.GetMethod("Function");
                        var functionConfiguration =
                            functionMethod.Invoke(instanceCallMethod, new object[] { operationMethodInfo.Name }) as
                                FunctionConfiguration;

                        if (functionConfiguration != null)
                        {
                            if (operationMethodInfo.IsComposable)
                            {
                                functionConfiguration.IsComposable = true;
                            }

                            AddParameters(functionConfiguration, operationMethodInfo);
                            AddFunctionReturn(
                                functionConfiguration, returnType, builder, operationMethodInfo.EntitySet);
                        }
                    }
                    else
                    {
                        var actionMethod = typeContainedMethod.GetMethod("Action");
                        var actionConfiguration = actionMethod.Invoke(
                            instanceCallMethod,
                            new object[] { operationMethodInfo.Name }) as ActionConfiguration;

                        if (actionConfiguration != null)
                        {
                            AddParameters(actionConfiguration, operationMethodInfo);
                            AddActionReturn(actionConfiguration, returnType, builder, operationMethodInfo.EntitySet);
                        }
                    }
                }
            }
        }

        private class OperationMethodInfo
        {
            public MethodInfo Method { get; set; }

            public OperationAttribute OperationAttribute { get; set; }

            public string Name
            {
                get { return this.OperationAttribute.Name ?? this.Method.Name; }
            }

            public string Namespace
            {
                get { return this.OperationAttribute.Namespace; }
            }

            public string EntitySet
            {
                get { return this.OperationAttribute.EntitySet; }
            }

            public bool IsComposable
            {
                get { return this.OperationAttribute.IsComposable; }
            }

            public bool IsBound
            {
                get { return this.OperationAttribute.IsBound; }
            }

            public bool HasSideEffects
            {
                get { return this.OperationAttribute.HasSideEffects; }
            }
        }
    }
}
