using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SpeachBingCore.Models;

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
            //   IBlobStorageRespository blob = new BlobStorageRespositary();
            //   CloudBlockBlob blob = container.GetBlockBlobReference(data.Uri.ToString());
            //      blob.UploadBlob(postedFile);
         //   postedFile.SaveAs(Path.Combine(@"c:\AZ", "audio.wav"));
            postedFile.SaveAs(Path.Combine(Server.MapPath("~/Resources/"), "audio.webm"));
            return View("Index");
        }
        [HttpPost]
        //lime 
        //ffmpeg
        public async Task<ActionResult> Index(BingApi ba)
        {
            //   string path = Path.Combine(Server.MapPath("~/Resources/"), "whatstheweatherlike.wav");
            string pathIn = Path.Combine(Server.MapPath("~/Resources/"), "audio.webm");
            string pathOut = Path.Combine(Server.MapPath("~/Resources/"), "audio.wav");
            Process.Start(@"C:\ffmpeg\ffmpeg.exe", $"-y -i {pathIn} -acodec pcm_u8 -ar 48000 {pathOut}").WaitForExit(1000);
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

    }
}