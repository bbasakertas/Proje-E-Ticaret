using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.Filter;

namespace ProjeETicaret.Controllers
{
    [MyAuthorization(_memberType: 8)]
    public class CategoryController : BaseController
    {
        // GET: Category
        public ActionResult i()
        {
            var cats = Context.Categories.Where(x => x.IsDeleted == false || x.IsDeleted == null).ToList();
            return View(cats.OrderByDescending(x => x.AddedDate).ToList());
        }
        public ActionResult Edit(int id = 0)
        {
            var cat = Context.Categories.FirstOrDefault(x => x.Id == id);
            var categories = Context.Categories.Select(x => new SelectListItem()
            {
                Text = x.Name,
                Value = x.Id.ToString()
            }).ToList();
            categories.Add(new SelectListItem()
            {
                Value = "0",
                Text = "Ana Kategori",
                Selected = true
            });
            ViewBag.Categories = categories;
            return View(cat);
        }
        [HttpPost]
        public ActionResult Edit(DB.Categories category)
        {
            if (category.Id > 0)
            {
                var cat = Context.Categories.FirstOrDefault(x => x.Id == category.Id);
                cat.Description = category.Description;
                cat.Name = category.Name;
                cat.ModifiedDate = DateTime.Now;
                cat.IsDeleted = false;

                if (category.Parent_Id > 0)
                    cat.Parent_Id = category.Parent_Id;
                else
                    cat.Parent_Id = null;

            }
            else
            {
                category.AddedDate = DateTime.Now;
                category.IsDeleted = false;
                if (category.Parent_Id == 0)
                    category.Parent_Id = null;
                Context.Entry(category).State = System.Data.Entity.EntityState.Added;

            }
            Context.SaveChanges();
            return RedirectToAction("i");

        }
        public ActionResult Delete(int id)
        {
            var pro = Context.Categories.FirstOrDefault(x => x.Id == id);
            pro.IsDeleted = true;
            Context.SaveChanges();
            return RedirectToAction("i");

        }
    }
}