using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace EQArchitect.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Required)]
    public class GetController : Controller
    {
        // GET: Get
        public void Index()
        {
            Response.Redirect("~");
        }

        public string SpellName(object ID)
        {
            EQArchitect.MvcApplication.NotReturningHTML();

            string _name = DB.GetText("SELECT name FROM spells_new WHERE id=" + DB.ToInt(ID).ToString());

            if (_name == "")
            {
                _name = string.Format("Unknown Spell # {0}", ID);
            }

            return _name;
        }

        public string ItemName(object ID)
        {
            EQArchitect.MvcApplication.NotReturningHTML();

            string _name = DB.GetText("SELECT name FROM items WHERE id=" + DB.ToInt(ID).ToString());

            if (_name == "")
            {
                _name = string.Format("Unknown Item # {0}", ID);
            }

            return _name;
        }

        public string RaceName(object ID)
        {
            EQArchitect.MvcApplication.NotReturningHTML();

            return EQInfo.Races.Name(DB.ToInt(ID));
        }

        public ActionResult EQInfoJS()
        {
            EQArchitect.MvcApplication.NotReturningHTML();

            // Cache the Javascript if no changes have been made to any of the information in it since the last call.

            string _modifiedHeader = Request.Headers["If-Modified-Since"];
            if (_modifiedHeader != null)
            {
                DateTime _clientVersion = DateTime.Parse(_modifiedHeader).AddSeconds(1).ToUniversalTime();

                if (_clientVersion >= EQInfo.ListsLastUpdated)
                {
                    return new HttpStatusCodeResult(304, "Page has not been modified");
                }
            }

            // Compress the Javascript as it's going across the internet. Drops the file size down to about 20%.
            string _acceptedEncodings = Request.Headers["Accept-Encoding"];
            if (_acceptedEncodings != null)
            {
                _acceptedEncodings = _acceptedEncodings.ToUpper();
                if (_acceptedEncodings.Contains("GZIP"))
                {
                    Response.AppendHeader("Content-encoding", "gzip");
                    Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress);
                }
                else if (_acceptedEncodings.Contains("DEFLATE"))
                {
                    Response.AppendHeader("Content-encoding", "deflate");
                    Response.Filter = new DeflateStream(Response.Filter, CompressionMode.Compress);
                }
            }
            
            return PartialView("EQInfo.js");
        }
    }
}