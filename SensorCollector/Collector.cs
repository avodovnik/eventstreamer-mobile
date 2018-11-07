using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace SensorCollector
{
    public class Collector : IDisposable
    {
        private string _token = null;

        public Collector()
        {

        }


        private void GenerateToken()
        {
            var resourceUri = $"{UserPreferences.Namespace}.servicebus.windows.net/{UserPreferences.EventHubName}";
            var keyName = UserPreferences.KeyName;
            var keyValue = UserPreferences.KeyValue;

            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 7;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keyValue));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);

            this._token = sasToken;
        }

        public void Send()
        {
            // if not sending
            if (!_isSending)
            {
                // start
                this.Start();
            }
            // then send
        }

        public void Start()
        {
            _isSending = true;

            _thread = new Thread(DoSendingInternal);
            _thread.Start();

        }

        private async void DoSendingInternal()
        {
            var uri = $"https://{UserPreferences.Namespace}.servicebus.windows.net/{UserPreferences.EventHubName}/messages?timeout=60&api-version=2014-01";
            var contentType = "application/vnd.microsoft.servicebus.json";

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                while (_isSending)
                {
                    if (_token == null) { GenerateToken(); }

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", _token);

                    string[] buffer;

                    lock (_buffer)
                    {
                        buffer = new string[_buffer.Count];
                        _buffer.CopyTo(buffer);
                        _buffer.Clear();
                    }

                    // send the request
                    System.Diagnostics.Debug.WriteLine($"Sending a batch of {buffer.Length} events.");

                    var payload = string.Join(",", buffer.Select(x => $"{{\"Body\":{x}}}"));
                    payload = $"[{payload}]";

                    var re = await httpClient.PostAsync(uri, new StringContent(payload));
                    if((int)re.StatusCode != 201) {
                        // panic
                        System.Diagnostics.Debug.WriteLine(re.Content);
                        System.Diagnostics.Debug.WriteLine(re.StatusCode);
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        public void Finish()
        {
            this.Dispose();
        }


        public void Dispose()
        {
            _isSending = false;
            _thread = null;
        }

        private bool _isSending = false;
        private Thread _thread;
        private List<string> _buffer = new List<string>();

        internal void PushEvent(string v)
        {
            lock (_buffer)
            {
                _buffer.Add(v);
            }
        }
    }
}
