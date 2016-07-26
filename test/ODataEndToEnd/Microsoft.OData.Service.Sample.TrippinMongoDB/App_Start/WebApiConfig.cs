using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.OData.Service.Sample.MongoDB.Api;
using Microsoft.Restier.Publishers.OData.Batch;
using Microsoft.Restier.Publishers.OData.Routing;

namespace Microsoft.OData.Service.Sample.MongoDB
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.MapRestierRoute<TrippinMongoDbApi>(
                "TrippinApi", "api/Trippin",
                new RestierBatchHandler(GlobalConfiguration.DefaultServer));
        }
    }
}
