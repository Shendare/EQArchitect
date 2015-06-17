using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EQArchitect.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Required)]
    public class RulesController : Controller
    {
        // GET: Rules
        public ActionResult Index()
        {
            return View();
        }
    }
}