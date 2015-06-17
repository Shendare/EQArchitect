using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace EQArchitect
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "SpellsByPage",
                url: "Spells/page{id}",
                defaults: new { controller = "Spells", action = "Index", classnick = "" },
                constraints: new { id = @"\d+" }
            );

            routes.MapRoute(
                name: "SpellsForClassByPage",
                url: "Spells/{classNick}/page{id}",
                defaults: new { controller = "Spells", action = "Index" },
                constraints: new { id = @"\d+", classNick = @"[A-Za-z]{3}" }
            );

            routes.MapRoute(
                name: "SpellsForClass",
                url: "Spells/{classNick}",
                defaults: new { controller = "Spells", action = "Index", id = -1 },
                constraints: new { classNick = @"[A-Za-z]{3}" }
            );

            routes.MapRoute(
                name: "SpellByID",
                url: "Spells/{id}",
                defaults: new { controller = "Spells", action = "Edit", classnick = "" },
                constraints: new { id = @"\d+" }
            );

            routes.MapRoute(
                name: "SpellForClass",
                url: "Spells/{id}/{classNick}",
                defaults: new { controller = "Spells", action = "Edit" },
                constraints: new { id = @"\d+", classNick = @"[A-Za-z]{3}" }
            );

            routes.MapRoute(
                name: "Connect",
                url: "Connect",
                defaults: new { controller = "Home", action = "Connect", id = -1 }
            );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = -1 }
            );
        }
    }
}
