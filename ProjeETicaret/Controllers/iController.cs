using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.DB;
using ProjeETicaret.Filter;
using ProjeETicaret.Models;
using ProjeETicaret.Models.i;

namespace ProjeETicaret.Controllers
{
    public class iController : BaseController
    {
        ProjeETicaretDBEntities db = new ProjeETicaretDBEntities();
        // GET: i
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Index(int id = 0) //Anasayfa
        {
            
            
            IQueryable<DB.Products> products = Context.Products.OrderByDescending(x => x.AddedDate).Where(x => x.IsDeleted == false || x.IsDeleted == null);
            //products' ı aldım
            DB.Categories category = null;
            if (id > 0) 
            {
                category = Context.Categories.FirstOrDefault(x => x.Id == id); //x id'si gönderdiğim id'ye eşitse
                var allCategories = GetChildCategories(category); //alt kategori yani çocuk kategori oluşturma
                allCategories.Add(category); //oluşturulan çocuk(child) kategoriyi ekle

                var catIntList = allCategories.Select(x => x.Id).ToList(); //Kategori seçme
                //select * from Product where Category_Id in (1,2,3,4)
                products = products.Where(x => catIntList.Contains(x.Category_Id)); //ürünün hangi kategoriden olduğunu içerir
            }
            var viewModel = new Models.i.IndexModel()
            {
                Products = products.ToList(), //ürün listesi
                Category = category //kategoriler
            };

           
            return View(viewModel); //sayfaya gönder

        }

        public ActionResult Search(string q) //ÜRÜN ARAMA
        {
            var p = db.Products.Where(i => i.IsDeleted == false); //aradığım ürün silinmemiş ise
            if (!string.IsNullOrEmpty(q)) //gönderdiğim parametre boş değilse
            {
                p = p.Where(i => i.Name.Contains(q) || i.Description.Contains(q)); //aradığım ürünün adı ve açıklaması
            }
            return View(p.ToList()); //Listeleme
        }

        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Product(int id = 0) //ÜRÜNLER
        {
            var pro = Context.Products.FirstOrDefault(x => x.Id == id); //ürün oluşturma ()

            if (pro == null) return RedirectToAction("index", "i"); //eğer ürün yoksa anasayfaya yönlendir

            ProductModels model = new ProductModels()
            {
                Product = pro,
                Comments = pro.Comments.ToList() //Yorumu listesi
            };
            return View(model);
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        [MyAuthorization] //Kişiye özel
        public ActionResult Product(DB.Comments comment) //YORUM YAPMA
        {
            try
            {
                comment.Member_Id = base.CurrentUserId();
                comment.AddedDate = DateTime.Now; //AddedDate : Eklenme Tarihi
                Context.Comments.Add(comment); //Yorum ekle
                Context.SaveChanges(); //Yapılanları kaydetme
            }
            catch (Exception ex)
            {
                ViewBag.MyError = ex.Message; //Hata mesajı
            }
            return RedirectToAction("Product", "i"); //Ürünlere yönlendir
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult AddBasket(int id = 0, bool remove = false) //SEPETE EKLEME
        {
            List<Models.i.BasketModels> basket = null;
            if (Session["Basket"] == null) //Sepet boş ise 
            {
                basket = new List<Models.i.BasketModels>(); //Sepet listesi oluşturma
            }
            else //Sepet boş değilse
            {
                basket = (List<Models.i.BasketModels>)Session["Basket"]; //Bu Listeye ekle
            }

            if (basket.Any(x => x.Product.Id == id)) //x product id'si gönderdiğim id'ye eşit
            {
                var pro = basket.FirstOrDefault(x => x.Product.Id == id);
                if (remove && pro.Count > 0) //eğer silinmiş ve ürün sayısı 0'dan büyükse
                {
                    pro.Count -= 1; //ürün sayısını 1 azalt çünkü stoktan eksilmesi lazım
                }
                else
                {
                    if (pro.Product.UnitsInStock > pro.Count) //Ürünün stok sayısı oluşturduğum ürün sayısından fazlaysa
                    {
                        pro.Count += 1; //ürün sayısını 1 arttır
                    }
                    else
                    {
                        TempData["MyError"] = "Yeterli Stok yok"; //Hiç stok yoksa
                    }
                }

            }
            else
            {
                var pro = Context.Products.FirstOrDefault(x => x.Id == id); // oluşturulan ürün ilk veya varsayılan
                if (pro != null && pro.IsContinued && pro.UnitsInStock > 0) //ürün null değilse ve hala devam ediyorsa ve stok sayısı 0'dan fazla ise
                {
                    basket.Add(new Models.i.BasketModels() //sepete ekle
                    {
                        Count = 1, //sayısı 1
                        Product = pro
                    });
                }
                else if (pro != null && pro.IsContinued == false) //ürün null değilse ve satışı devam etmiyorsa
                {
                    TempData["MyError"] = "Bu ürünün satışı durduruldu."; //Hata mesajı : Bu ürünün satışı durduruldu
                }
            }
            basket.RemoveAll(x => x.Count < 1); //Sepettekilerin hepsini silme
            Session["Basket"] = basket;

            return RedirectToAction("Basket", "i"); //Sepet sayfasına yönlendir
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Basket()
        {
            List<Models.i.BasketModels> model = (List<Models.i.BasketModels>)Session["Basket"];
            if (model == null)
            {
                model = new List<Models.i.BasketModels>();
            }
            if (base.IsLogon())
            {
                int currentId = CurrentUserId();
                ViewBag.CurrentAddresses = Context.Addresses
                                            .Where(x => x.Member_Id == currentId)
                                            .Select(x => new SelectListItem()
                                            {
                                                Text = x.Name,
                                                Value = x.Id.ToString()
                                            }).ToList();
            }
            ViewBag.TotalPrice = model.Select(x => x.Product.Price * x.Count).Sum();

            return View(model);
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult RemoveBasket(int id) //SEPETTEN SİLME
        {
            List<Models.i.BasketModels> basket = (List<Models.i.BasketModels>)Session["Basket"];
            if (basket != null) //sepet null değilse
            {
                if (id > 0)
                {
                    basket.RemoveAll(x => x.Product.Id == id); //sepetteki ürünlerin hepsini sil
                }
                else if (id == 0)
                {
                    basket.Clear(); //sepet boş yani temizlenmiş
                }
                Session["Basket"] = basket;
            }
            return RedirectToAction("Basket", "i"); //Sepet sayfasına yönlendir
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        public ActionResult Buy(string Address) //SATIN ALMA
        {
            if (IsLogon()) //kullanıcı giriş yapmış ise
            {
                try
                {
                    var basket = (List<Models.i.BasketModels>)Session["Basket"];
                    var guid = new Guid(Address); //kullanıcının adres bilgisi
                    var _address = Context.Addresses.FirstOrDefault(x => x.Id == guid); //adres ilk veya varsayılan
                    //Sipariş Verildi = SV
                    //Ödeme Bildirimi = OB
                    //Ödeme Onaylandı = OO

                    var order = new DB.Orders() //Sipariş oluşturma
                    {
                        AddedDate = DateTime.Now, //Eklenme tarihi 
                        Address = _address.AdresDescription, //Adres açıklaması
                        Member_Id = CurrentUserId(), //Kullanıcı
                        Status = "SV", //sipariş verildi
                        Id = Guid.NewGuid()
                    };
                    
                    foreach (Models.i.BasketModels item in basket) //Bu döngü içinde işlemler yapılsın
                    {
                        var oDetail = new DB.OrderDetails(); //Sipariş Detayı oluşturuldu
                        oDetail.AddedDate = DateTime.Now; //Eklenme tarihi
                        oDetail.Price = item.Product.Price * item.Count; //Sipariş fiyatı -> Ürün fiyatı * üründen kaç tane alındığı
                        oDetail.Product_Id = item.Product.Id; //Ürün id'si
                        oDetail.Quantity = item.Count; //Quantity : miktar
                        oDetail.Id = Guid.NewGuid();

                        order.OrderDetails.Add(oDetail); //Siparişi ekle

                        var _product = Context.Products.FirstOrDefault(x => x.Id == item.Product.Id); //oluşturulan ürün ilk veya varsayılan
                        if (_product != null && _product.UnitsInStock >= item.Count) // eğer ürün null değilse ve ürünün stok sayısı ürün miktarından fazlaysa
                        {
                            _product.UnitsInStock = _product.UnitsInStock - item.Count; //ürün stok sayısından miktarı azalt
                        }
                        else
                        {
                            throw new Exception(string.Format("{0} ürünü için yeterli stok yoktur veya silinmiş bir ürünü almaya çalışıyorsunuz.", item.Product.Name));
                        }
                    }
                    Context.Orders.Add(order);
                    Context.SaveChanges(); //Yapılanları kaydetme
                    Session["Basket"] = null;
                }
                catch (Exception ex)
                {
                    TempData["MyError"] = ex.Message; //Hata mesajı
                }
                return RedirectToAction("Buy", "i"); //Satış sayfasına yönlendir
            }
            else //Kullanıcı giriş yapmamış ise
            {
                return RedirectToAction("Login", "Account"); //Giriş Yapma sayfasına yönlendir
            }

        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        [MyAuthorization] //Kişiye özel
        public ActionResult Buy() //SATIN ALMA
        {
            if (IsLogon())
            {
                var currentId = CurrentUserId();
                IQueryable<DB.Orders> orders;
                if (((int)CurentUser().MemberType) > 8)
                {
                    orders = Context.Orders.Where(x => x.Status == "OB");
                }
                else
                {
                    orders = Context.Orders.Where(x => x.Member_Id == currentId);
                }

                List<Models.i.BuyModels> model = new List<BuyModels>();
                foreach (var item in orders)
                {
                    var byModel = new BuyModels();
                    byModel.TotelPrice = item.OrderDetails.Sum(y => y.Price);
                    byModel.OrderName = string.Join(", ", item.OrderDetails.Select(y => y.Products.Name + "(" + y.Quantity + ")"));
                    byModel.OrderStatus = item.Status;
                    byModel.OrderId = item.Id.ToString();
                    byModel.Member = item.Members;
                    model.Add(byModel);
                }

                return View(model);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

        }

        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        [MyAuthorization] //Kişiye özel
        public JsonResult OrderNotification(OrderNotificationModel model) //SİPARİŞ BİLDİRİMİ
        {
            if (string.IsNullOrEmpty(model.OrderId) == false) //eğer sipariş id yoksa boş ise
            {
                var guid = new Guid(model.OrderId); //id ataması yap
                var order = Context.Orders.FirstOrDefault(x => x.Id == guid); //Sipariş oluşturma
                if (order != null) //sipariş null değil ise
                {
                    order.Description = model.OrderDescription; //Sipariş açıklaması
                    order.Status = "OB"; //Ödeme Bildirimi
                    Context.SaveChanges(); //Yapılanları kaydet
                }
            }
            return Json("");
        }

        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        public JsonResult GetProductDes(int id)
        {
            var pro = Context.Products.FirstOrDefault(x => x.Id == id);
            return Json(pro.Description, JsonRequestBehavior.AllowGet);
        }

        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public JsonResult GetOrder(string id) //SİPARİŞ GETİRME
        {
            var guid = new Guid(id); //id ataması (terminalden)
            var order = Context.Orders.FirstOrDefault(x => x.Id == guid); //sipariş ilk veya varsayılan (x id'si guid'ye eşit)
            return Json(new
            {
                Description = order.Description, //Sipariş açıklaması
                Address = order.Address //Adres
            }, JsonRequestBehavior.AllowGet); //json isteği -> izin ver (admin izin verecek)
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        [MyAuthorization] //Kişiye özel
        public JsonResult OrderCompilete(string id, string text) //SİPARİŞ TAMAMLAMA
        {
            var guid = new Guid(id); //id ataması (terminalden)
            var order = Context.Orders.FirstOrDefault(x => x.Id == guid); //sipariş ilk veya varsayılan (x id'si guid'ye eşit)
            order.Description = text; //Sipariş açıklaması -> text
            order.Status = "OO"; //Ödeme Onaylandı
            Context.SaveChanges(); //Yapılanları kaydet
            return Json(true, JsonRequestBehavior.AllowGet); //json isteği -> izin ver (admin izin verecek)
        }
        [HttpGet]
        public ActionResult AddFavorities(int id) //ürün id'sini alıyorum
        {
            List<Models.i.FavoritiesModels> favori = null;
           
            if(Session["Favori"] == null)
            {
                favori = new List<Models.i.FavoritiesModels>();
            }
            else
            {
                favori = (List<Models.i.FavoritiesModels>)Session["Favori"];
            }

            if(favori.Any(x => x.Product.Id == id)) //böyle bir key varsa eğer;
            {
               
                var pro = favori.FirstOrDefault(x => x.Product.Id == id);
                pro.Count += 1;
            }
            else
            {
                var pro = Context.Products.FirstOrDefault(x => x.Id == id);
                if(pro != null)
                {
                    favori.Add(new Models.i.FavoritiesModels()
                    {
                        Count = 1,
                        Product = pro
                    });
                }
            }
            Session["Favori"] = favori;

            return RedirectToAction("Favori", "i");
           
        }
        [HttpGet]
        public ActionResult Favori()
        {
            List<Models.i.FavoritiesModels> model = (List<Models.i.FavoritiesModels>)Session["Favori"];
           
            if(model == null)
            {
                model = new List<Models.i.FavoritiesModels>();
            }

            
            return View(model);
        }
        public ActionResult RemoveFavori(int id) //FAVORİLERDEN SİLME
        {
            List<Models.i.FavoritiesModels> favori = (List<Models.i.FavoritiesModels>)Session["Favori"];
            if (favori != null) //favori null değilse
            {
                if (id > 0)
                {
                    favori.RemoveAll(x => x.Product.Id == id); //favorilerdeki ürünlerin hepsini sil
                }
                else if (id == 0)
                {
                    favori.Clear(); //favori boş yani temizlenmiş
                }
                Session["Favori"] = favori;
            }
            return RedirectToAction("Favori", "i"); //Favori sayfasına yönlendir
        }
        [HttpGet]
        public ActionResult AddCompare(int id) //ürün id'sini alıyorum
        {
            List<Models.i.CompareModels> compare = null; //Karşılaştırma listesi

            if (Session["Compare"] == null) //Session: Kullanıcı giriş yapmadan da karşılaştırma yapabilir
            {
                compare = new List<Models.i.CompareModels>();  //karşılaştırma için liste oluştur
            }
            else
            {
                compare = (List<Models.i.CompareModels>)Session["Compare"]; //zaten karşılaştırma listesi doluysa oraya git
            }

            if (compare.Any(x => x.Product.Id == id)) //böyle bir key varsa eğer;
            {

                var pro = compare.FirstOrDefault(x => x.Product.Id == id); //ilk varsayılan x için x'in Product_Is'si, gelen id'ye eşitse
                pro.Count += 1;
            }
            else //ilk varsayılan x için x'in Product_Is'si, gelen id'ye eşit değilse
            {
                var pro = Context.Products.FirstOrDefault(x => x.Id == id); //ilk varsayılan x oluştur ve x'in id'si gelen id'ye eşitse
                if (pro != null)
                {
                    compare.Add(new Models.i.CompareModels() //Karşılaştırmaya ekle
                    {
                        Count = 1,
                        Product = pro
                    });
                }
            }
            Session["Compare"] = compare; //Karşılaştırma sayfası güncelleme

            return RedirectToAction("Compare", "i"); //Redirect To Action : Eylemi Yönlendirme
            //Ürünü karşılaştırmaya ekledikten sonra karşılaştırma sayfasına yönlendirme
        }
            
        [HttpGet]
        public ActionResult Compare()
        {
            List<Models.i.CompareModels> model = (List<Models.i.CompareModels>)Session["Compare"]; //Karşılaştırma listesi

            if (model == null) //model null ise
            {
                model = new List<Models.i.CompareModels>(); //yeni oluştur
            }
            return View(model); //değilse modeli dön
        }
        public ActionResult RemoveCompare(int id) //KARŞILAŞTIRMALARDAN SİLME
        {
            List<Models.i.CompareModels> compare = (List<Models.i.CompareModels>)Session["Compare"];
            if (compare != null) //karşılaştırma null değilse
            {
                if (id > 0)
                {
                    compare.RemoveAll(x => x.Product.Id == id); //karşılaştırmadaki ürünlerin hepsini sil
                }
                else if (id == 0)
                {
                    compare.Clear(); //karşılaştırma boş yani temizlenmiş
                }
                Session["Compare"] = compare;
            }
            return RedirectToAction("Compare", "i"); //karşılaştırma sayfasına yönlendir
        }

    }
}