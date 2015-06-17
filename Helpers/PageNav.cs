using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EQArchitect
{
    public class PageNav
    {
        public int Start = 1;
        public int Count = 0;
        public int PerPage = 20;
        public string BasePage = ".";

        public int Last
        {
            get
            {
                return Math.Min(Count, Start + PerPage - 1);
            }
        }

        public int Page
        {
            get
            {
                return (Count == 0) ? 0 : (int)Math.Floor((Start - 1) / (float)PerPage) + 1;
            }
        }

        public int Pages
        {
            get
            {
                return (Count == 0) ? 0 : (int)Math.Ceiling(Count / (float)PerPage);
            }
        }
    }

}