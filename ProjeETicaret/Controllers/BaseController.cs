using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.DB;

namespace ProjeETicaret.Controllers
{
    //MİRAS ALINACAK SINIF
    public class BaseController : Controller
    {
        protected ProjeETicaretDBEntities Context { get; private set; } //protected : sadece miras alanlar görsün
                                                                 //get : değer okuma! set: değer atama! private set : sadece p sınıf içerisinde değer atama!
        public BaseController()
        {
            Context = new ProjeETicaretDBEntities();
            ViewBag.MenuCategories = Context.Categories.Where(x => x.Parent_Id == null).ToList();
            
        }
        protected DB.Members CurentUser()
        {
            if (Session["LogonUser"] == null) return null;
            return (DB.Members)Session["LogonUser"];
        }
        protected int CurrentUserId()
        {
            if (Session["LogonUser"] == null) return 0;
            return ((DB.Members)Session["LogonUser"]).Id;
        }
        protected bool IsLogon()
        {
            if (Session["LogonUser"] == null)
                return false;
            else
                return true;
        }
        /// <summary>
        /// tüm alt kategorielri getirir
        /// </summary>
        /// <param name="cat">Hangi kategorinin alt kategorilerini getirsin.</param>
        /// <returns></returns>
        protected List<Categories> GetChildCategories(Categories cat)
        {
            var result = new List<Categories>();


            result.AddRange(cat.Categories1);
            foreach (var item in cat.Categories1)
            {
                var list = GetChildCategories(item);
                result.AddRange(list);
            }

            return result;

        }
    }
}