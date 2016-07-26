// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using MongoDB.Driver;

namespace Microsoft.Restier.Providers.MongoDB.Query
{
    /// <summary>
    /// Represents a query expression sourcer that uses a Database.
    /// </summary>
    internal class QueryExpressionSourcer : IQueryExpressionSourcer
    {
        /// <summary>
        /// Sources an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <param name="embedded">
        /// Indicates if the sourcing is occurring on an embedded node.
        /// </param>
        /// <returns>
        /// A data source expression that represents the visited node.
        /// </returns>
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            // TODO, need some check to only source expression needed by MongoDB....
            var entitySet = context.ModelReference.EntitySet;
            if (entitySet == null)
            {
                // EF provider can only source *EntitySet*.
                return null;
            }

            var entitySetName = entitySet.Name;

            Type elementCLRType;
            var mapper = context.QueryContext.GetApiService<IModelMapper>();
            mapper.TryGetRelevantType(context.QueryContext.ApiContext, entitySetName, out elementCLRType);
            var database = context.QueryContext.GetApiService<IMongoDatabase>();

            var method = typeof(IMongoDatabase).GetMethod("GetCollection").MakeGenericMethod(elementCLRType);
            var instance = Expression.Constant(database);
            var parameter = Expression.Constant(entitySetName);
            var nullConstant = Expression.Constant(null, typeof(MongoCollectionSettings));
            Expression callExpr = Expression.Call(instance, method, parameter, nullConstant);


            method = typeof(IMongoCollectionExtensions).GetMethod("AsQueryable").MakeGenericMethod(elementCLRType);

            callExpr = Expression.Call(null, method, callExpr);
            
            return callExpr;
        }
    }
}
