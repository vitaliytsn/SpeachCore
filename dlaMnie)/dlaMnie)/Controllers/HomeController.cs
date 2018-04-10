
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

        MqttClient client;
        string clientId;



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
            //starting mosquito broker
            string BrokerAddress = "test.mosquitto.org";
            clientId = Guid.NewGuid().ToString();
            client = new MqttClient(BrokerAddress);
            client.Connect(clientId);
            //waiting the response from bing
            while (ba._logText == null);
            ViewBag.logs = ba._logText;
            //seting the chanel
            string Topic = "Speach";
            // publish a message with QoS 2
            client.Publish(Topic, Encoding.UTF8.GetBytes(ba._logText), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            return View("Index");
        }
    }
}
