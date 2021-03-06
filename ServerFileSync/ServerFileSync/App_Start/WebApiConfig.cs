﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ServerFileSync
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DeleteMethod",
                routeTemplate: "api/{controller}/{filename}/{extension}"//,
                //defaults: new { control = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
