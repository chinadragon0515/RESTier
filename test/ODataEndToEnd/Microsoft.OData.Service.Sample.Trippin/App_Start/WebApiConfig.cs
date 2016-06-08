// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.OData;
using Microsoft.OData.Service.Sample.Trippin.Api;
using Microsoft.OData.Service.Sample.Trippin.Security;
using Microsoft.Restier.Publishers.OData.Batch;
using Microsoft.Restier.Publishers.OData.Routing;

namespace Microsoft.OData.Service.Sample.Trippin
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // For security demo only, Login controller will use this route
            config.Routes.MapHttpRoute(
                name: "API Default",
                routeTemplate: "test/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            RegisterTrippin(config, GlobalConfiguration.DefaultServer);
            config.MessageHandlers.Add(new ETagMessageHandler());
            config.Filters.Add(new CustomizedAuthenticationFilter());
            config.Filters.Add(new CustomizedAuthorizeAttribute());
        }

        public static async void RegisterTrippin(
            HttpConfiguration config, HttpServer server)
        {
            await config.MapRestierRoute<TrippinApi>(
                "TrippinApi", "api/Trippin",
                new RestierBatchHandler(server));
        }
    }
}
