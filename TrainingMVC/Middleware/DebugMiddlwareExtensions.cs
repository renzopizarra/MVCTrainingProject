using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingMVC.Middleware
{
    public static class DebugMiddlwareExtensions
    {
        public static void UseDebugMiddleware(this IAppBuilder app, DebugMiddlewareOptions options = null)
        {
            if (options == null)
                options = new DebugMiddlewareOptions();

            app.Use<DebugMiddleware>(options);
        }
    }
}