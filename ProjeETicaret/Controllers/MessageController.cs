using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjeETicaret.Filter;
using ProjeETicaret.Models.Message;

namespace ProjeETicaret.Controllers
{
    [MyAuthorization]
    public class MessageController : BaseController
    {
        // GET: Message
        [HttpGet]
        public ActionResult i()
        {
            if (IsLogon() == false) return RedirectToAction("index", "i");
            var currentId = CurrentUserId();
            Models.Message.iModels model = new Models.Message.iModels();
            #region - Select List Item -
            model.Users = new List<SelectListItem>();
            var users = Context.Members.Where(x => ((int)x.MemberType) > 0 && x.Id != currentId).ToList();
            model.Users = users.Select(x => new SelectListItem()
            {
                Value = x.Id.ToString(),
                Text = string.Format("{0} {1} ({2})", x.Name, x.Surname, x.MemberType.ToString())
            }).ToList();
            #endregion
            #region - Mesaj Listesi -
            var mList = Context.Messages.Where(x => x.ToMemberId == currentId || x.MessageReplies.Any(y => y.Member_Id == currentId)).ToList();
            model.Messages = mList;
            #endregion
            return View(model);
        }
        [HttpPost]
        public ActionResult SendMessage(Models.Message.SendMessageModel message)
        {
            if (IsLogon() == false) return RedirectToAction("index", "i");

            DB.Messages mesaj = new DB.Messages()
            {
                Id = Guid.NewGuid(),
                AddedDate = DateTime.Now,
                IsRead = false,
                Subject = message.Subject,
                ToMemberId = message.ToUserId
            };
            var mRep = new DB.MessageReplies()
            {
                Id = Guid.NewGuid(),
                AddedDate = DateTime.Now,
                Member_Id = CurrentUserId(),
                Text = message.MessageBody
            };
            mesaj.MessageReplies.Add(mRep);
            Context.Messages.Add(mesaj);
            Context.SaveChanges();
            return RedirectToAction("i", "Message");
        }
        [HttpGet]
        public ActionResult MessageReplies(string id)
        {
            if (IsLogon() == false) return RedirectToAction("index", "i");
            var currentId = CurrentUserId();
            var guid = new Guid(id);
            ProjeETicaret.DB.Messages message = Context.Messages.FirstOrDefault(x => x.Id == guid);
            if (message.ToMemberId == currentId)
            {
                message.IsRead = true;
                Context.SaveChanges();
            }

            MessageRepliesModel model = new MessageRepliesModel();
            model.MReplies = Context.MessageReplies.Where(x => x.MessageId == guid).OrderBy(x => x.AddedDate).ToList();
            return View(model);
        }
        [HttpPost]
        public ActionResult MessageReplies(DB.MessageReplies message)
        {
            if (IsLogon() == false) return RedirectToAction("index", "i");

            message.AddedDate = DateTime.Now;
            message.Id = Guid.NewGuid();
            message.Member_Id = CurrentUserId();
            Context.MessageReplies.Add(message);
            Context.SaveChanges();
            return RedirectToAction("MessageReplies", "Message", new { id = message.MessageId });
        }

        [HttpGet]
        public ActionResult RenderMessage()
        {
            RenderMessageModel model = new RenderMessageModel();
            var currentId = CurrentUserId();
            var mList = Context.Messages
                        .Where(x => x.ToMemberId == currentId || x.MessageReplies.Any(y => y.Member_Id == currentId))
                        .OrderByDescending(x => x.AddedDate);
            model.Messages = mList.Take(4).ToList();
            model.Count = mList.Count();

            return PartialView("_Message", model);
        }

        public ActionResult RemoveMessageReplies(string id)
        {
            var guid = new Guid(id);
            //mesja cepaları silindi
            var mReplies = Context.MessageReplies.Where(x => x.MessageId == guid);
            Context.MessageReplies.RemoveRange(mReplies);
            //mesajın kendisi silindi.
            var message = Context.Messages.FirstOrDefault(x => x.Id == guid);
            Context.Messages.Remove(message);

            Context.SaveChanges();

            return RedirectToAction("i", "Message");
        }
    }
}