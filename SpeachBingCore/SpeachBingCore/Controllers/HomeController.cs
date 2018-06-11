using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using SpeachBingCore.Models;
using Vote.BL;

namespace SpeachBingCore.Controllers
{
    public class HomeController : Controller
    {

      
        public HomeController()
        {
           
        }

        public BingApi ba;
        public ActionResult Index()
        {
            
               ba = new BingApi();
            ViewBag.logs = ba._logText;
            return View();
        }
        public ActionResult Error(Exception e)
        {
            return View();
        }
        [HttpPost]
        public ActionResult Action(HttpPostedFileBase postedFile)
        {
            string serverPath = Server.MapPath("~/Resources");
            var audioFile = Path.Combine(serverPath, "audio.webm");
            System.IO.File.WriteAllText(Path.Combine(serverPath, "BeforeAudioSaveAs.txt"),$"{postedFile.ContentLength}, type:{postedFile.ContentType}, name:{postedFile.FileName}");
            postedFile.SaveAs(audioFile);
            System.IO.File.WriteAllText(Path.Combine(serverPath, "AfterAudioSaveAs.txt"), $"{audioFile}");
            return View("Index");
        }

        [HttpPost]
        //lime 
        //ffmpeg
        public async Task<ActionResult> Index(BingApi ba)
        {
         //      string pathIn = Path.Combine(Server.MapPath("~/Resources/"), "audio.webm");
            // string pathIn = (Path.Combine(@"c:\AZ", "audio.webm"));
         //   string pathOut = Path.Combine(Server.MapPath("~/Resources/"), "audio.wav");
            string serverPath = Server.MapPath("~/Resources");
            string pathIn = Path.Combine(serverPath, "audio.webm");
            string pathOut = Path.Combine(serverPath, "audio.wav");
            string converterPath = Path.Combine(serverPath, "ffmpeg","ffmpeg.exe");
            Process.Start(converterPath, $"-y -i {pathIn} -acodec pcm_u8 -ar 48000 {pathOut}").WaitForExit(1000);

            try
            {
                await ba.StartButton_Click(pathOut);
            }
            catch (Exception e)
            {
                ba._logText = e.ToString();
            }

            ViewBag.logs = "you can start to speak";
            while (ba._logText == null);
            ViewBag.logs = ba._logText;
            return View("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Dupex()
        {
           ApiCommunicator ac = new ApiCommunicator();
           await ac.SendVoice(ViewBag.logs);
            ViewBag.Answer = await ac.Answer();
            return RedirectToAction("Index");
        }

    }
}