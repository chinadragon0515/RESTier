// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using MongoDB.Driver;


namespace Microsoft.Restier.Providers.MongoDB
{
    /// <summary>
    /// Represents an API over a MongoDB.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class MongoDbApi : ApiBase
    {
        /// <summary>
        /// Gets the underlying Database for this API.
        /// </summary>
        protected IMongoDatabase Database
        {
            get
            {
                return this.Context.GetApiService<IMongoDatabase>();
            }
        }

        /// <summary>
        /// Configures the API services for this API. Descendants may override this method to register
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            Type apiType = this.GetType();

            // Add core and convention's services
            services = services.AddCoreServices(apiType)
                .AddAttributeServices(apiType)
                .AddConventionBasedServices(apiType);

            // Add EF related services
            services.AddMongoDbProviderServices();

            // This is used to add the publisher's services
            ApiConfiguration.GetPublisherServiceCallback(apiType)(services);

            return services;
        }
    }
}
