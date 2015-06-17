using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EQArchitect.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Connect()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Connect")]
        [ValidateAntiForgeryToken]
        public ActionResult DoConnect()
        {
            if (Request.Form["UseDefault"] == "0")
            {
                DB.MakeConnectionString(Request.Form["DBServer"], DB.ToInt(Request.Form["DBPort"]), Request.Form["DBDatabase"], Request.Form["DBUsername"], Request.Form["DBPassword"]);
            }
            else
            {
                DB.MakeConnectionString();
            }

            return Redirect("~/");
        }
    }
}