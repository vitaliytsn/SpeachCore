using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.CognitiveServices.SpeechRecognition;


namespace noweGowno.Models
{
    public class BingApi : INotifyPropertyChanged
    {
        private const string IsolatedStorageSubscriptionKeyFileName = "Subscription.txt";

        private const string DefaultSubscriptionKeyPromptMessage = "Paste your subscription key here to start";

        private string subscriptionKey;

        private MicrophoneRecognitionClient micClient;

        public string _logText;
       
     
        public BingApi()
        {
            this.Initialize();
        }

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Events

        public bool IsMicrophoneClientShortPhrase { get; set; }

        public bool IsMicrophoneClientWithIntent { get; set; }

        public string SubscriptionKey
        {
            get
            {
                return this.subscriptionKey;
            }

            set
            {
                this.subscriptionKey = value;
                this.OnPropertyChanged<string>();
            }
        }

        private string LuisEndpointUrl
        {
            get { return ConfigurationManager.AppSettings["LuisEndpointUrl"]; }
        }

        private bool UseMicrophone
        {
            get
            {
                return this.IsMicrophoneClientWithIntent ||

                    this.IsMicrophoneClientShortPhrase;
            }
        }

        private bool WantIntent
        {
            get
            {
                return !string.IsNullOrEmpty(this.LuisEndpointUrl) &&
                    (this.IsMicrophoneClientWithIntent);
            }
        }

        private SpeechRecognitionMode Mode
        {
            get
            {
                return SpeechRecognitionMode.ShortPhrase;
            }
        }

        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        private string ShortWaveFile
        {
            get
            {
                return ConfigurationManager.AppSettings["ShortWaveFile"];
            }
        }

        private string LongWaveFile
        {
            get
            {
                return ConfigurationManager.AppSettings["LongWaveFile"];
            }
        }

        private string AuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["AuthenticationUri"];
            }
        }
        private static void SaveSubscriptionKeyToIsolatedStorage(string subscriptionKey)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                using (var oStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName, FileMode.Create, isoStore))
                {
                    using (var writer = new StreamWriter(oStream))
                    {
                        writer.WriteLine(subscriptionKey);
                    }
                }
            }
        }

        private void Initialize()
        {
            this.IsMicrophoneClientShortPhrase = true;
            this.IsMicrophoneClientWithIntent = false;
            this.SubscriptionKey = this.GetSubscriptionKeyFromIsolatedStorage();
        }

        public async void StartButton_Click()
        {
            SubscriptionKey = "ad6c48f014334ef6842cf5159ad34069";
            this.LogRecognitionStart();
            if (this.micClient == null)
            {
                if (this.WantIntent)
                {
                    this.CreateMicrophoneRecoClientWithIntent();
                }
                else
                {
                    this.CreateMicrophoneRecoClient();
                }
            }

            this.micClient.StartMicAndRecognition();

        }

        private void LogRecognitionStart()
        {
            string recoSource;
            if (this.UseMicrophone)
            {
                recoSource = "microphone";
            }
            else if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                recoSource = "short wav file";
            }
            else
            {
                recoSource = "long wav file";
            }

            //      this.WriteLine("\n--- Start speech recognition using " + recoSource + " with " + this.Mode + " mode in " + this.DefaultLocale + " language ----\n\n");
        }

     
        private void CreateMicrophoneRecoClient()
        {
            this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                this.Mode,
                this.DefaultLocale,
                this.SubscriptionKey);
            this.micClient.AuthenticationUri = this.AuthenticationUri;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            }


            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void CreateMicrophoneRecoClientWithIntent()
        {
            //  this.WriteLine("--- Start microphone dictation with Intent detection ----");

            this.micClient =
                SpeechRecognitionServiceFactory.CreateMicrophoneClientWithIntentUsingEndpointUrl(
                    this.DefaultLocale,
                    this.SubscriptionKey,
                    this.LuisEndpointUrl);
            this.micClient.AuthenticationUri = this.AuthenticationUri;
            this.micClient.OnIntent += this.OnIntentHandler;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {

            //      this.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            this.micClient.EndMicAndRecognition();

            this.WriteResponseResult(e);



        }

        private void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {

            //   this.WriteLine("--- OnDataShortPhraseResponseReceivedHandler ---");

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            this.WriteResponseResult(e);
        }

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                //    this.WriteLine("No phrase response is available.");
            }
            else
            {
                // this.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    this.WriteLine(
                        "Text=\"{2}\"",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);
                    this.WriteLine();
                }

                this.WriteLine();
            }
        }

        private void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            //  this.WriteLine("--- Intent received by OnIntentHandler() ---");
            //     this.WriteLine("{0}", e.Payload);
            //   this.WriteLine();
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            //     this.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            // Thread.Sleep(100);
            //   this.WriteLine("{0}", e.PartialResult);
            // this.WriteLine();
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {


            this.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            this.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            this.WriteLine("Error text: {0}", e.SpeechErrorText);
            this.WriteLine();
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {

            //     WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
            //       WriteLine("********* Microphone status: {0} *********", e.Recording);
            if (e.Recording)
            {
                //     WriteLine("Please start speaking.");
            }

            //   WriteLine();

        }

        private void WriteLine()
        {
            this.WriteLine(string.Empty);
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            _logText += (formattedStr + "\n");
        }

        private string GetSubscriptionKeyFromIsolatedStorage()
        {
            string subscriptionKey = null;

            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName, FileMode.Open, isoStore))
                    {
                        using (var reader = new StreamReader(iStream))
                        {
                            subscriptionKey = reader.ReadLine();
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    subscriptionKey = null;
                }
            }

            if (string.IsNullOrEmpty(subscriptionKey))
            {
                subscriptionKey = DefaultSubscriptionKeyPromptMessage;
            }

            return subscriptionKey;
        }

        private void SaveKey_Click(object sender)
        {
            try
            {
                SaveSubscriptionKeyToIsolatedStorage(this.SubscriptionKey);
                //     MessageBox.Show("Subscription key is saved in your disk.\nYou do not need to paste the key next time.", "Subscription Key");
            }
            catch (Exception exception)
            {
                /*      MessageBox.Show(
                          "Fail to save subscription key. Error message: " + exception.Message,
                          "Subscription Key",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);*/
            }
        }

        private void DeleteKey_Click(object sender)
        {
            try
            {
                this.SubscriptionKey = DefaultSubscriptionKeyPromptMessage;
                SaveSubscriptionKeyToIsolatedStorage(string.Empty);
                //   MessageBox.Show("Subscription key is deleted from your disk.", "Subscription Key");
            }
            catch (Exception exception)
            {
                /* MessageBox.Show(
                     "Fail to delete subscription key. Error message: " + exception.Message,
                     "Subscription Key",
                     MessageBoxButton.OK,
                     MessageBoxImage.Error);*/
            }
        }

        private void OnPropertyChanged<T>([CallerMemberName]string caller = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }

        private void RadioButton_Click(object sender)
        {
            // Reset everything
            if (this.micClient != null)
            {
                this.micClient.EndMicAndRecognition();
                this.micClient.Dispose();
                this.micClient = null;
            }

        
        }
    }
}