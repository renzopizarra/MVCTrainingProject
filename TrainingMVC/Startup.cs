using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using TrainingMVC.Middleware;
using Nancy.Owin;
using Nancy;
using System.Web.Http;

namespace TrainingMVC
{
    public class Startup
    {
        public static void Configuration(IAppBuilder app)
        {
            app.UseDebugMiddleware(new DebugMiddlewareOptions
            {
                OnIncomingRequest = (ctx) =>
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    ctx.Environment["DebugStopwatch"] = watch;
                },
                OnOutgoingRequest = (ctx) =>
                {
                    var watch = (Stopwatch)ctx.Environment["DebugStopwatch"];
                    watch.Stop();
                    Debug.WriteLine("Request took: " + watch.ElapsedMilliseconds + " ms");


                }
            });
          
            //For user authentication
            app.UseCookieAuthentication(new Microsoft.Owin.Security.Cookies.CookieAuthenticationOptions {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new Microsoft.Owin.PathString("/Authentication/Login")
            });


            //***Facebook authentication
            //app.UseFacebookAuthentication(new Microsoft.Owin.Security.Facebook.FacebookAuthenticationOptions
            //{
            //    AppId = "403019470412539",
            //    AppSecret = "c2968baf93ea8a54033543ca4ed56632",
            //    SignInAsAuthenticationType = "ApplicationCookie"
            //});


            app.Use(async (ctx, next) =>
            {
                if (ctx.Authentication.User.Identity.IsAuthenticated)
                    Debug.WriteLine("User: " + ctx.Authentication.User.Identity.Name);
                else
                    Debug.WriteLine("User is not authenticated"); 
                await next();
            });

            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);

            app.Map("/nancy", mappedApp => { mappedApp.UseNancy(); });
            //app.UseNancy(conf => {
            //    conf.PassThroughWhenStatusCodesAre(HttpStatusCode.NotFound);
            //});
         
           

            //    app.Use(async (ctx, next) =>
            //{
            //    await ctx.Response.WriteAsync("<html><head></head><body>Hello World</body></html>");
            //});
        }
    }
}