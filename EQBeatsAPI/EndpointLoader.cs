using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace EQBeatsAPI {
    public class EndpointLoader<T> {
        public delegate void EndpointLoadCompletedHandler(T data);
        public delegate void EndpointLoadFailedHandler(WebException exception);
        public event EndpointLoadCompletedHandler Complete;
        public event EndpointLoadFailedHandler Failure;

        private readonly HttpWebRequest _request;

        public EndpointLoader(Uri endpoint) {
            _request = WebRequest.CreateHttp(endpoint);
            _request.Accept = "application/json";
        }

        public void Begin() {
            _request.BeginGetResponse(OnGetResponse, null);
        }

        private void OnGetResponse(IAsyncResult ar) {
            try {
                var response = _request.EndGetResponse(ar);
                string responseString;
                using (var responseStream = response.GetResponseStream())
                using (var responseReader = new StreamReader(responseStream)) {
                    responseString = responseReader.ReadToEnd();
                }
                var responseObject = JsonConvert.DeserializeObject<T>(responseString);
                if (Complete != null) Complete(responseObject);
            } catch (WebException ex) {
                if (Failure != null) Failure(ex);
            }
        }
    }
}
