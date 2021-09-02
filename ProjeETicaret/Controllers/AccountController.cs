
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.Filter;
using ProjeETicaret.Models.Account;

namespace ProjeETicaret.Controllers
{
    public class AccountController : BaseController
    {
        // GET: Account
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        public ActionResult Register(Models.Account.RegisterModels user) //KAYDOLMA
        {
            try //hata yakalama
            {
                if (user.rePassword != user.Member.Password) //iki şifre aynı değilse
                {
                    throw new Exception("Şifreler aynı değil");
                }
                if (Context.Members.Any(x => x.Email == user.Member.Email))
                {
                    throw new Exception("Zaten bu e-posta adresi kayıtlı.");
                }
                user.Member.MemberType = DB.MemberTypes.Customer;
                user.Member.AddedDate = DateTime.Now; //eklenme tarihi
                Context.Members.Add(user.Member); //database'ye eklemek üzere sıraya koy
                Context.SaveChanges(); //database insert sorgusunu çalıştır
                return RedirectToAction("Login", "Account"); //kaydolduktan sonra Login sayfasına gitme

            }
            catch (Exception ex)
            {
                ViewBag.ReError = ex.Message; //View kısmının Register.cshtml sınıfında hata mesajı yazıldı
                return View();
            }

        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Login() //GİRİŞ YAPMA
        {
            return View();
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        public ActionResult Login(Models.Account.LoginModels model) //giriş yapan kullanıcı
        {
            try
            {
                var user = Context.Members.FirstOrDefault(x => x.Password == model.Member.Password && x.Email == model.Member.Email);
                //Eğer böyle bir kullanıcı varsa
                if (user != null) //Kullanıcı yoksa
                {
                    Session["LogonUser"] = user; //Login işlemi olduysa
                    return RedirectToAction("index", "i"); //anasayfaya (indexe) yönlendir
                }
                else
                {
                    ViewBag.ReError = "Kullanıcı Bilgileriniz yanlış"; //Hata mesajı
                    return View(); //sayfayı yönlendir
                }
            }
            catch (Exception ex)
            {
                ViewBag.ReError = ex.Message;
                return View();
            }
        }
        public ActionResult Logout() //ÇIKIŞ YAPMA
        {
            Session["LogonUser"] = null; //kullanıcı yoksa yani kullanıcı çıkış yapmış ise
            return RedirectToAction("Login", "Account"); //Login sayfasına yönlendir
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult Profil(int id = 0, string ad = "")
        {
            List<DB.Addresses> addresses = null;
            DB.Addresses currentAddress = new DB.Addresses(); //Veritabanından adres nesnesi oluşturuldu
            if (id == 0)
            {
                id = base.CurrentUserId();
                addresses = Context.Addresses.Where(x => x.Member_Id == id).ToList(); //gönderdiğim id Member_Id'ye eşitse listeye ekle
                if (!string.IsNullOrEmpty(ad)) //isim boş değilse
                {
                    var guild = new Guid(ad); //Guid : terminalden id değeri alabiliyoruz
                    currentAddress = Context.Addresses.FirstOrDefault(x => x.Id == guild); //x id'si terminalden aldığımız guiD değerine eşit mi
                }
            }
            var user = Context.Members.FirstOrDefault(x => x.Id == id); //Kullanıcı oluşturma (x id'di gönderdiğim id'ye eşitse)
            if (user == null) return RedirectToAction("index", "i"); // index sayfasına yönlendirme
            ProfilModels model = new ProfilModels()
            {
                Members = user,
                Addresses = addresses,
                CurrentAddress = currentAddress
            };
            return View(model);
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        [MyAuthorization]
        public ActionResult ProfilEdit() //PROFİL DÜZENLEME
        {
            int id = base.CurrentUserId();
            var user = Context.Members.FirstOrDefault(x => x.Id == id); //Kullanıcı oluşturma (x id'di gönderdiğim id'ye eşitse)
            if (user == null) return RedirectToAction("index", "i"); // index sayfasına yönlendirme
            ProfilModels model = new ProfilModels()
            {
                Members = user
            };
            return View(model);
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        [MyAuthorization]
        public ActionResult ProfilEdit(ProfilModels model) //PROFİL DÜZENLEME
        {
            try
            {
                int id = CurrentUserId();
                var updateMember = Context.Members.FirstOrDefault(x => x.Id == id); //Kullanıcı güncelleme (x id'di gönderdiğim id'ye eşitse)
                updateMember.ModifiedDate = DateTime.Now; //ModifiedDate : değiştirilme zamanı
                updateMember.Bio = model.Members.Bio; //Biografi
                updateMember.Name = model.Members.Name; //Ad güncelleme
                updateMember.Surname = model.Members.Surname; //Soyad güncelleme

                if (string.IsNullOrEmpty(model.Members.Password) == false) //Şifre yoksa-yanlışsa
                {
                    updateMember.Password = model.Members.Password; //şifre güncelleme
                }
                if (Request.Files != null && Request.Files.Count > 0) //Fotoğraf Güncelleme
                {
                    var file = Request.Files[0]; //Dosya oluşturma
                    if (file.ContentLength > 0)
                    {
                        var folder = Server.MapPath("~/images/upload");
                        var fileName = Guid.NewGuid() + ".jpg";
                        file.SaveAs(Path.Combine(folder, fileName));

                        var filePath = "images/upload/" + fileName; //Dosya yolu alma
                        updateMember.ProfileImageName = filePath;
                    }
                }
                Context.SaveChanges(); //Değişikleri kaydetme

                return RedirectToAction("Profil", "Account"); //Profil sayfasına yönlendirme
            }
            catch (Exception ex) //Hata var ise hata mesajı
            {
                ViewBag.MyError = ex.Message;
                int id = CurrentUserId();
                var viewModel = new Models.Account.ProfilModels()
                {
                    Members = Context.Members.FirstOrDefault(x => x.Id == id)
                };
                return View(viewModel);
            }
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        [MyAuthorization] //Kişiye özel
        public ActionResult Address(DB.Addresses address) //ADRESLER
        {
            DB.Addresses _address = null;
            if (address.Id == Guid.Empty) //Adres yok ise
            {
                address.Id = Guid.NewGuid(); //guid ile adres nesnesi
                address.AddedDate = DateTime.Now; //Eklenme tarihi
                address.Member_Id = base.CurrentUserId();
                Context.Addresses.Add(address); //Adres ekleme
            }
            else //Adres var ise
            {
                _address = Context.Addresses.FirstOrDefault(x => x.Id == address.Id); //gönderdiğim x id'si adress id'sine eşit
                _address.ModifiedDate = DateTime.Now; //ModifiedDate : değiştirilme zamanı -> now(şimdi)
                _address.Name = address.Name; //Adres ismi
                _address.AdresDescription = address.AdresDescription; //Adres açıklaması
            }
            Context.SaveChanges(); //Yapılanları kaydet
            return RedirectToAction("Profil", "Account"); //Profil sayfasına yönlendir
        }

        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        [MyAuthorization]
        public ActionResult RemoveAddress(string id)
        {
            var guid = new Guid(id);
            var address = Context.Addresses.FirstOrDefault(x => x.Id == guid);
            Context.Addresses.Remove(address); //adres silmek
            Context.SaveChanges(); //Yapılanları kaydetme
            return RedirectToAction("Profil", "Account"); //Profil sayfasına yönlendirme
        }
        [HttpGet] //Bir sayfaya girdiği zaman ilk önce get'i çalışsın
        public ActionResult ForgotPassword() //Şifremi unuttum
        {
            return View();
        }
        [HttpPost] //Bir sayfayı kaydetmek için yolladıysam yani kaydol butonuna tıkladıysam da post olarak gelsin
        public ActionResult ForgotPassword(string email)
        {
            var member = Context.Members.FirstOrDefault(x => x.Email == email); //x emaili gönderdiğim emaile eşit
            if (member == null) //üye yoksa
            {
                ViewBag.MyError = "Böyle bir hesap bulunamadı"; //Hata mesajı -> Böyle bir hesap yok
                return View();
            }
            else //üye varsa
            {
                var body = "Şifreniz : " + member.Password;
                 //MyMail mail = new MyMail(member.Email, "Şifremi Unuttum", body);
                 //mail.SendMail();
                TempData["Info"] = email + " mail adresinize şifreniz gönderilmiştir.";
                return RedirectToAction("Login");
            }

        }
    }
}

