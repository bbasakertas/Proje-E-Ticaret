using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.DB;
using ProjeETicaret.Filter;

namespace ProjeETicaret.Controllers
{
    [MyAuthorization(_memberType: 8)]
    public class ProductController : BaseController
    {
        // GET: Product
        ProjeETicaretDBEntities db = new ProjeETicaretDBEntities();
        public ActionResult i(string ara)
        {
            
           
            var products = Context.Products.Where(x => x.IsDeleted == false || x.IsDeleted == null).ToList();
            return View(products.OrderByDescending(x => x.AddedDate).ToList());
        }

        
        public ActionResult Edit(int id = 0)
        {
            var product = Context.Products.FirstOrDefault(x => x.Id == id);
            var categories = Context.Categories.Select(x => new SelectListItem()
            {
                Text = x.Name,
                Value = x.Id.ToString()
            }).ToList();
            ViewBag.Categories = categories;
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(DB.Products product)
        {
            var productImagePath = string.Empty;
            if (Request.Files != null && Request.Files.Count > 0)
            {
                var file = Request.Files[0];
                if (file.ContentLength > 0)
                {
                    var folder = Server.MapPath("~/images/upload/Product");
                    var fileName = Guid.NewGuid() + ".jpg";
                    file.SaveAs(Path.Combine(folder, fileName));

                    var filePath = "images/upload/Product/" + fileName;
                    productImagePath = filePath;
                }
            }
            if (product.Id > 0)
            {
                var dbProduct = Context.Products.FirstOrDefault(x => x.Id == product.Id);
                dbProduct.Category_Id = product.Category_Id;
                dbProduct.ModifiedDate = DateTime.Now;
                dbProduct.Description = product.Description;
                dbProduct.IsContinued = product.IsContinued;
                dbProduct.Name = product.Name;
                dbProduct.Price = product.Price;
                dbProduct.UnitsInStock = product.UnitsInStock;
                dbProduct.IsDeleted = false;
                if (string.IsNullOrEmpty(productImagePath) == false)
                {
                    dbProduct.ProductImageName = productImagePath;
                }
            }
            else
            {
                product.AddedDate = DateTime.Now;
                product.IsDeleted = false;
                product.ProductImageName = productImagePath;
                Context.Entry(product).State = System.Data.Entity.EntityState.Added;

            }

            Context.SaveChanges();

            return RedirectToAction("i");
        }

        public ActionResult Delete(int id)
        {
            var pro = Context.Products.FirstOrDefault(x => x.Id == id);
            pro.IsDeleted = true;
            Context.SaveChanges();
            return RedirectToAction("i");
        }
    }
}
