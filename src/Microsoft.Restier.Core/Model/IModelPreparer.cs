// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// The service to prepare for model generation.
    /// </summary>
    public interface IModelPreparer
    {
        /// <summary>
        /// Asynchronously prepare model generation for an API.
        /// </summary>
        /// <param name="context">
        /// The context for processing
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task PrepareModelAsync(ModelContext context, CancellationToken cancellationToken);
    }
}
