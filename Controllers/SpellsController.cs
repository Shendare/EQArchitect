using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using EQArchitect.Models;

namespace EQArchitect.Controllers
{
    public class SpellsController : Controller
    {
        // GET: Spells
        public ActionResult Index(string ClassNick, int ID)
        {
            return View("SpellsList", new Spells(Spells.SpellsAction.Index, ClassNick, ID).Parameters);
        }

        public ActionResult Edit(string ClassNick, int ID)
        {
            return View("SpellEdit", new Spells(Spells.SpellsAction.Edit, ClassNick, ID).Parameters);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult Save(string ClassNick, int ID)
        {
            string _result = Spells.Save(System.Web.HttpContext.Current.Request);

            Spells.SpellsParameters _spell = new Spells(Spells.SpellsAction.Edit, ClassNick, ID).Parameters;

            _spell.SaveStatus = _result;

            return View("SpellEdit", _spell);
        }
    }
}