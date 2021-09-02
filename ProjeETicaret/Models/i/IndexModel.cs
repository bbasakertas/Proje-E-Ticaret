using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ProjeETicaret.DB;

namespace ProjeETicaret.Models.i
{
    public class IndexModel
    {
        public List<DB.Products> Products { get; set; }
        public DB.Categories Category { get; set; }
        //public List<Products> Products { get; internal set; }
    }
}