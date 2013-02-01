using System;
using System.Collections.Generic;
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
    public class ResourceAcquirer<T> {
        public delegate void AcquisitionCompleteHandler(T result);
        public delegate void AcquisitionFailedHandler(WebException exception);
        public event AcquisitionCompleteHandler Complete;
        public event AcquisitionFailedHandler Failure;

        private readonly int _id;
        private readonly HttpWebRequest _request;
        private readonly Dictionary<int, T> _cache;
        private readonly Dictionary<int, ResourceAcquirer<T>> _acquirers;

        public ResourceAcquirer(int id, Uri target, Dictionary<int, T> cache,
                          Dictionary<int, ResourceAcquirer<T>> acquirers) {
            _id = id;
            _cache = cache;
            _acquirers = acquirers;
            _request = WebRequest.CreateHttp(target);
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
                lock (_cache) {
                    _cache[_id] = responseObject;
                    _acquirers.Remove(_id);
                    if (Complete != null) Complete(responseObject);
                }
            } catch (WebException ex) {
                lock (_cache) {
                    _acquirers.Remove(_id);
                    if (Failure != null) Failure(ex);
                }
            }
        }
    }
}
