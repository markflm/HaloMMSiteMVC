using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Net;
namespace HaloMMSiteMVC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            //trying to fix exceptions in gameDetails fetcher

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
    | SecurityProtocolType.Tls11
    | SecurityProtocolType.Tls
    | SecurityProtocolType.Ssl3;

            

            ServicePointManager.DefaultConnectionLimit = 5;

        }
    }
}
