using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.SpeechRecognition;

namespace SpeachBingCore.Models
{
    public class BingApi : INotifyPropertyChanged
    {
        private const string IsolatedStorageSubscriptionKeyFileName = "Subscription.txt";

        private const string DefaultSubscriptionKeyPromptMessage = "Paste your subscription key here to start";

        public string _logText;
        private DataRecognitionClient dataClient;

        private MicrophoneRecognitionClient micClient;
        private string Path = "";

        private string subscriptionKey;


        public BingApi()
        {
            Initialize();
        }

        public bool IsDataClientShortPhrase { get; set; }
        public bool IsMicrophoneClientDictation { get; set; }
        public bool IsDataClientDictation { get; set; }

        public bool IsDataClientWithIntent { get; set; }

        public bool IsMicrophoneClientShortPhrase { get; set; }

        public bool IsMicrophoneClientWithIntent { get; set; }

        public string SubscriptionKey
        {
            get => subscriptionKey;

            set
            {
                subscriptionKey = value;
                OnPropertyChanged<string>();
            }
        }

        private string LuisEndpointUrl => ConfigurationManager.AppSettings["LuisEndpointUrl"];

        private bool UseMicrophone => IsMicrophoneClientWithIntent ||
                                      IsMicrophoneClientShortPhrase;

        private bool WantIntent => !string.IsNullOrEmpty(LuisEndpointUrl) &&
                                   IsMicrophoneClientWithIntent;

        private SpeechRecognitionMode Mode
        {
            get
            {
                if (IsMicrophoneClientDictation ||
                    IsDataClientDictation)
                    return SpeechRecognitionMode.LongDictation;

                return SpeechRecognitionMode.ShortPhrase;
            }
        }

        private string DefaultLocale => "en-US";

        private string ShortWaveFile => ConfigurationManager.AppSettings["ShortWaveFile"];

        private string LongWaveFile => ConfigurationManager.AppSettings["LongWaveFile"];

        private string AuthenticationUri => ConfigurationManager.AppSettings["AuthenticationUri"];

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        private static void SaveSubscriptionKeyToIsolatedStorage(string subscriptionKey)
        {
            using (var isoStore =
                IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                using (var oStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName,
                    FileMode.Create, isoStore))
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
            IsMicrophoneClientShortPhrase = false;
            IsMicrophoneClientWithIntent = false;
            IsMicrophoneClientDictation = false;
            IsDataClientShortPhrase = true;
            IsDataClientWithIntent = false;
            IsDataClientDictation = false;
            SubscriptionKey = GetSubscriptionKeyFromIsolatedStorage();
        }

        public async Task StartButton_Click(string path)
        {
            Path = path;
            SubscriptionKey = "8c3ae34bb802411392ec8ed1fffa9dee";

            LogRecognitionStart();

            if (UseMicrophone)
            {
                if (micClient == null)
                    if (WantIntent)
                        CreateMicrophoneRecoClientWithIntent();
                    else
                        CreateMicrophoneRecoClient();

                micClient.StartMicAndRecognition();
            }
            else
            {
                if (null == dataClient)
                    if (WantIntent)
                        CreateDataRecoClientWithIntent();
                    else
                        CreateDataRecoClient();

                SendAudioHelper(Mode == SpeechRecognitionMode.ShortPhrase ? Path : LongWaveFile);
            }
        }

        private void SendAudioHelper(string wavFileName)
        {
            using (var fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                var bytesRead = 0;
                var buffer = new byte[1024];

                try
                {
                    do
                    {
                        // Get more Audio data to send into byte buffer.
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        // Send of audio data to service. 
                        dataClient.SendAudio(buffer, bytesRead);
                    } while (bytesRead > 0);
                }
                finally
                {
                    // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                    dataClient.EndAudio();
                }
            }
        }

        private void CreateDataRecoClient()
        {
            dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            dataClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            if (Mode == SpeechRecognitionMode.ShortPhrase)
                dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;

            dataClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            dataClient.OnConversationError += OnConversationErrorHandler;
        }

        private void CreateDataRecoClientWithIntent()
        {
            dataClient = SpeechRecognitionServiceFactory.CreateDataClientWithIntentUsingEndpointUrl(
                DefaultLocale,
                SubscriptionKey,
                LuisEndpointUrl);
            dataClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;
            dataClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            dataClient.OnConversationError += OnConversationErrorHandler;

            // Event handler for intent result
            dataClient.OnIntent += OnIntentHandler;
        }

        private void LogRecognitionStart()
        {
            string recoSource;
            if (UseMicrophone)
                recoSource = "microphone";
            else if (Mode == SpeechRecognitionMode.ShortPhrase)
                recoSource = "short wav file";
            else
                recoSource = "long wav file";

            //      this.WriteLine("\n--- Start speech recognition using " + recoSource + " with " + this.Mode + " mode in " + this.DefaultLocale + " language ----\n\n");
        }


        private void CreateMicrophoneRecoClient()
        {
            micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            micClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            if (Mode == SpeechRecognitionMode.ShortPhrase)
                micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;


            micClient.OnConversationError += OnConversationErrorHandler;
        }

        private void CreateMicrophoneRecoClientWithIntent()
        {
            //  this.WriteLine("--- Start microphone dictation with Intent detection ----");

            micClient =
                SpeechRecognitionServiceFactory.CreateMicrophoneClientWithIntentUsingEndpointUrl(
                    DefaultLocale,
                    SubscriptionKey,
                    LuisEndpointUrl);
            micClient.AuthenticationUri = AuthenticationUri;
            micClient.OnIntent += OnIntentHandler;

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            micClient.OnConversationError += OnConversationErrorHandler;
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            //      this.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            micClient.EndMicAndRecognition();

            WriteResponseResult(e);
        }

        private void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            //   this.WriteLine("--- OnDataShortPhraseResponseReceivedHandler ---");

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            WriteResponseResult(e);
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
                for (var i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    WriteLine(
                        "{2}",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);
                    WriteLine();
                }

                WriteLine();
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
            WriteLine("--- Error received by OnConversationErrorHandler() ---");
            WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            WriteLine("Error text: {0}", e.SpeechErrorText);
            WriteLine();
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
            WriteLine(string.Empty);
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            _logText += formattedStr + "\n";
        }

        private string GetSubscriptionKeyFromIsolatedStorage()
        {
            string subscriptionKey = null;

            using (var isoStore =
                IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName,
                        FileMode.Open, isoStore))
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

            if (string.IsNullOrEmpty(subscriptionKey)) subscriptionKey = DefaultSubscriptionKeyPromptMessage;

            return subscriptionKey;
        }

        private void SaveKey_Click(object sender)
        {
            try
            {
                SaveSubscriptionKeyToIsolatedStorage(SubscriptionKey);
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
                SubscriptionKey = DefaultSubscriptionKeyPromptMessage;
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

        private void OnPropertyChanged<T>([CallerMemberName] string caller = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(caller));
        }

        private void RadioButton_Click(object sender)
        {
            // Reset everything
            if (micClient != null)
            {
                micClient.EndMicAndRecognition();
                micClient.Dispose();
                micClient = null;
            }
        }
    }
}