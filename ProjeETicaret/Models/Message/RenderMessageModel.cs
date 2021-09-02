using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjeETicaret.Models.Message
{
    public class RenderMessageModel
    {
        public List<DB.Messages> Messages { get; set; }
        public int Count { get; set; }
    }
}