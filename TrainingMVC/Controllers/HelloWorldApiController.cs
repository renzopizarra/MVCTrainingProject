using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace TrainingMVC.Controllers
{
    [RoutePrefix("api")]
    public class HelloWorldApiController: ApiController
    {
     [Route("Hello")]
     [HttpGet]
        public IHttpActionResult HelloWorld()
        {
            return Content(System.Net.HttpStatusCode.OK, "Hello from Web Api");
        }
    }
}