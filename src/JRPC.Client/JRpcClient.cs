using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JRPC.Client.Extensions;
using JRPC.Core;
using JRPC.Core.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace JRPC.Client {

    public class JRpcClient : IJRpcClient {

        private readonly string _endpoint;
        private readonly TimeSpan _timeout;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JRpcClient()
            : this("http://localhost:12345") {
        }

        public JRpcClient(string endpoint)
            : this(
                endpoint, new JsonSerializerSettings() {ContractResolver = new DefaultContractResolver()}
            ) {
        }

        public JRpcClient(string endpoint, JsonSerializerSettings jsonSerializerSettings)
            : this(endpoint, TimeSpan.FromHours(1), jsonSerializerSettings) {
            _endpoint = endpoint;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public JRpcClient(string endpoint, TimeSpan timeout, JsonSerializerSettings jsonSerializerSettings) {
            _endpoint = endpoint;
            _timeout = timeout;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        //TODO: удалить метод
        public Task<string> Call(string name, string method, string parameters, IAbstractCredentials credentials) {
            return InvokeRequest(GetEndPoint(name), method,
                JsonConvert.DeserializeObject(parameters, _jsonSerializerSettings), credentials);
        }

        public Task<TResult> Call<TResult>(string name, string method, object parameters, IAbstractCredentials credentials) {
            return InvokeRequest<TResult>(GetEndPoint(name), method, parameters, credentials);
        }

        public T GetProxy<T>(string taskName) where T : class {
            return GetProxy<T>(taskName, null);
        }

        public T GetProxy<T>(string taskName, IAbstractCredentials credentials) where T : class {
            var cacheKey = GetEndPoint(taskName);
            return JRpcStaticClientFactory.Get<T>(this, taskName, cacheKey, _jsonSerializerSettings, credentials);
        }

        private string GetEndPoint(string name) {
            return _endpoint + (_endpoint.EndsWith("/")
                       ? ""
                       : "/") + name;
        }

        private async Task<T> InvokeRequest<T>(string url, string method, object data, IAbstractCredentials credentials) {
            return JsonConvert.DeserializeObject<T>(await InvokeRequest(url, method, data, credentials).ConfigureAwait(false),
                _jsonSerializerSettings);
        }

        private async Task<string> InvokeRequest(string service, string method, object data, IAbstractCredentials credentials) {
            var id = new Random().Next();

            var request = new JRpcRequest {
                Id = id,
                Method = method.ToLowerInvariant(),
                Params = JToken.FromObject(data),
            };

            var content = await HttpAsyncRequest("POST",
                "application/json",
                service,
                JsonConvert.SerializeObject(request, _jsonSerializerSettings),
                _timeout, credentials).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content)) {
                throw new Exception("Не получили ответ от сервиса " + service);
            }

            var jsonresponse = JsonConvert.DeserializeObject<JRpcResponse>(content, _jsonSerializerSettings);

            if (jsonresponse.Error != null) {
                throw jsonresponse.Error;
            }

            return JsonConvert.SerializeObject(jsonresponse.Result, _jsonSerializerSettings);
        }

        /// <summary>
        /// Копипаста AsyncTools HttpAsyncRequester.Request
        /// </summary>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="timeout"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private static async Task<string> HttpAsyncRequest(string method, string contentType, string url,
            string requestBody, TimeSpan timeout, IAbstractCredentials credentials) {
            var request = (HttpWebRequest) WebRequest.Create(url);
            if (request.ServicePoint.ConnectionLimit < 100) {
                request.ServicePoint.ConnectionLimit = 100;
            }
            request.Method = method;
            request.ContentType = contentType;
            request.KeepAlive = true;
            request.Timeout = (int) timeout.TotalMilliseconds;
            request.ReadWriteTimeout = request.Timeout;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 5;

            if (credentials != null) {
                request.Headers.Add(HttpRequestHeader.Authorization, credentials.GetHeaderValue());
            }

            if (requestBody != null) {
                var bytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bytes.Length;
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
                    await requestStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            HttpWebResponse response;
            try {
                response =
                    (HttpWebResponse)
                    await request.GetResponseAsync().WithTimeout(request.Timeout).ConfigureAwait(false);
            } catch (WebException ex) {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    response = (HttpWebResponse) ex.Response;
                } else {
                    response = null;
                }
            } catch (TimeoutException ex) {
                response = null;
            }

            using (response) {
                var responceBytes = response != null
                    ? ReadBytesToEnd(response.GetResponseStream())
                    : new byte[0];
                var responceString = Encoding.UTF8.GetString(responceBytes);
                return responceString;
            }
        }

        /// <summary>
        /// Читаем из потока все байты
        /// </summary>
        /// <param name="stream">Поток</param>
        /// <returns>Массив прочтенных байтов</returns>
        /// <remarks>Внимание! поток будет закрыт!</remarks>
        public static byte[] ReadBytesToEnd(Stream stream) {
            //Размер блока читаемого из потока, в байтах
            const int BYTE_BLOCK_SIZE = 102400;

            var buffer = new byte[BYTE_BLOCK_SIZE];
            using (var ms = new MemoryStream()) {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }

}