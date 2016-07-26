// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !EF7
using System.Linq;
#endif
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Providers.MongoDB.Query
{
    /// <summary>
    /// Represents a query executor that uses Entity Framework methods.
    /// This class only executes queries against EF provider, it'll
    /// delegate other queries to inner IQueryExecutor.
    /// </summary>
    internal class QueryExecutor : IQueryExecutor
    {
        public IQueryExecutor Inner { get; set; }

        /// <summary>
        /// Asynchronously executes a query and produces a query result.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the query.
        /// </typeparam>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="query">
        /// A composed query.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            // IMongoQueryProvider is an internal class 
            if (query.Provider.GetType().Name == "MongoQueryProviderImpl`1")
            {
                var iqueryableResult = query.ToArray();
                var result = new QueryResult(iqueryableResult);
                return result;
            }

           return await Inner.ExecuteQueryAsync(context, query, cancellationToken);
        }

        /// <summary>
        /// Asynchronously executes a singleton
        /// query and produces a query result.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the singleton query result.
        /// </typeparam>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="queryProvider">
        /// A query provider to execute expression.
        /// </param>
        /// <param name="expression">
        /// An expression to be composed on the base query.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        public async Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
            // var provider = queryProvider as IMongoQueryProvider;
            // IMongoQueryProvider is an internal class 
            //if (queryProvider.GetType().Name == "IMongoQueryProvider")
            //{
            //    var result = await queryProvider.ExecuteAsync<TResult>(
            //        expression, cancellationToken);
            //    return new QueryResult(new TResult[] { result });
            //}

            return await Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
        }
    }
}
