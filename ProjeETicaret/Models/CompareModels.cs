using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjeETicaret.Models
{
    public class CompareModels
    {
        public CompareModels()
        {
            this.Products = new Dictionary<int, int>();
        }
        public Dictionary<int, int> Products { get; set; } //ilk int: alınan ürün (key). ikinci int: kaç tane olduğunu
    }
}