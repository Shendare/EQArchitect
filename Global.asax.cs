using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace EQArchitect
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpContext.Current.Items["renderStartTime"] = DateTime.Now;
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            EQInfo.CheckLists();
        }

        public static void NotReturningHTML()
        {
            HttpContext.Current.Items["renderStartTime"] = null;
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current.Items["renderStartTime"] != null)
            {
                DateTime start = (DateTime)HttpContext.Current.Items["renderStartTime"];
                TimeSpan renderTime = DateTime.Now - start;
                HttpContext.Current.Response.Write("<!-- Page Rendered At: " + DateTime.Now.ToUniversalTime().ToString("R") + ", Render Time: " + renderTime + " -->");
            }
        }

        protected void Application_PostRequestHandlerExecute(object sender, EventArgs e)
        {
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }
    }

    
}
