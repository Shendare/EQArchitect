using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
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

        public ActionResult Download()
        {
            Response.Clear();
            Response.ContentType = "octet/stream";
            Response.AppendHeader("content-disposition", "attachment; filename=spells_us.txt");

            using (OdbcDataReader _data = DB.OpenDataStream("SELECT * FROM spells_new ORDER BY id;"))
            {
                if ((_data != null) && (_data.HasRows))
                {
                    Object[] _row = new Object[_data.FieldCount];

                    while (_data.Read())
                    {
                        bool _firstField = true;
                        _data.GetValues(_row);

                        foreach (Object _value in _row)
                        {
                            if (_firstField)
                            {
                                _firstField = false;
                            }
                            else
                            {
                                Response.Write('^');
                            }

                            Response.Write(DB.ToText(_value));
                        }

                        Response.Write("\r\n");
                        Response.Flush();
                    }
                }
            }

            Response.End();

            return null;
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