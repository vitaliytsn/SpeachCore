using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using noweGowno.Models;

namespace WebApplication10.Controllers
{
    public class HomeController : Controller
    {
        public BingApi ba = new BingApi();
        public ActionResult Chat()
        {
            ViewBag.logs = ba._logText;
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Chat(BingApi ba)
        {
            List<Task> TaskList = new List<Task>();
            TaskList.Add(Task.Factory.StartNew(() => ba.StartButton_Click()));
            ViewBag.logs = "you can start to speak";
            Task.WaitAll(TaskList.ToArray());
            while (ba._logText == null) ;
            ViewBag.logs = ba._logText;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}