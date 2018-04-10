
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SpeachCore.Models;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SpeachCore.Controllers
{
    public class HomeController : Controller
    {

       public BingApi ba = new BingApi();
       
       


        public ActionResult Index()
        {

            ViewBag.logs = ba._logText;
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Index(BingApi ba)
        {
            List<Task> TaskList = new List<Task>();
            TaskList.Add(Task.Factory.StartNew(() => ba.StartButton_Click()));
            ViewBag.logs = "you can start to speak";
            Task.WaitAll(TaskList.ToArray());
         
            while (ba._logText == null) ;
            ViewBag.logs = ba._logText;            
           

            return View("Index");
            
        }
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
