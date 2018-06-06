using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace Vote.BL
{
    public class ApiCommunicator
    {
        private readonly HttpClient _client;

        public ApiCommunicator()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://80.211.151.64:45675") };
        }

        public async Task<bool> SendVoice(string voice)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(voice));
                var response = await _client.PostAsync("/voice", content);
                var json = await response.Content.ReadAsStringAsync();
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<string> Answer()
        {
            try
            {
                var response = await _client.GetAsync("/answer");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<string>(json);  
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
