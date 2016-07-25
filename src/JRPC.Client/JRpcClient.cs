using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using JRPC.Core;
using JRPC.Client.Extensions;

namespace JRPC.Client {

    public class JRpcClient : IJRpcClient {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly string _endpoint;

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JRpcClient()
            : this("http://localhost:12345") { }

        public JRpcClient(string endpoint)
            : this(endpoint, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }) { }

        public JRpcClient(string endpoint, JsonSerializerSettings jsonSerializerSettings) {
            _endpoint = endpoint;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        //TODO: удалить метод
        public Task<string> Call(string name, string method, string parameters) {
            return InvokeRequest(GetEndPoint(name), method, JsonConvert.DeserializeObject(parameters, _jsonSerializerSettings));
        }

        public Task<TResult> Call<TResult>(string name, string method, object parameters) {
            return InvokeRequest<TResult>(GetEndPoint(name), method, parameters);
        }

        public T GetProxy<T>(string taskName) where T : class {
            return JRpcStaticClientFactory.Get<T>(this, taskName, _jsonSerializerSettings);
        }

        private string GetEndPoint(string name) {
            return _endpoint + (_endpoint.EndsWith("/") ? "" : "/") + name;
        }

        private async Task<T> InvokeRequest<T>(string url, string method, object data) {
            return JsonConvert.DeserializeObject<T>(await InvokeRequest(url, method, data).ConfigureAwait(false), _jsonSerializerSettings);
        }

        private async Task<string> InvokeRequest(string service, string method, object data) {
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
                 TimeSpan.FromHours(1)).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content)) {
                throw new Exception("Не получили ответ от сервиса " + service);
            }

            var jsonresponse = JsonConvert.DeserializeObject<JRpcResponse>(content, _jsonSerializerSettings);

            if (jsonresponse.Error != null) {
                throw new Exception(jsonresponse.Error.ToString());
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
        /// <returns></returns>
        private static async Task<string> HttpAsyncRequest(string method, string contentType, string url, string requestBody, TimeSpan timeout) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (request.ServicePoint.ConnectionLimit < 100) {
                request.ServicePoint.ConnectionLimit = 100;
            }
            request.Method = method;
            request.ContentType = contentType;
            request.KeepAlive = true;
            request.Timeout = (int)timeout.TotalMilliseconds;
            request.ReadWriteTimeout = request.Timeout;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 5;

            if (requestBody != null) {
                byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bytes.Length;
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
                    await requestStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            HttpWebResponse response;
            try {
                response = (HttpWebResponse)await request.GetResponseAsync().WithTimeout(request.Timeout).ConfigureAwait(false);
            } catch (WebException ex) {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    response = (HttpWebResponse)ex.Response;
                } else {
                    response = null;
                }
            } catch (TimeoutException ex) {
                response = null;
            }

            using (response) {
                var responceBytes = response != null ? ReadBytesToEnd(response.GetResponseStream()) : new byte[0];
                var responceString = Encoding.UTF8.GetString(responceBytes);
                return responceString;
            }
        }

        /// <summary>
        /// Размер блока читаемого из потока, в байтах
        /// </summary>
        private const int BYTE_BLOCK_SIZE = 102400;

        /// <summary>
        /// Читаем из потока все байты
        /// Копипаста SolverCommon PageContentHelper.ReadBytesToEnd
        /// </summary>
        /// <param name="stream">Поток</param>
        /// <returns>Массив прочтенных байтов</returns>
        /// <remarks>Внимание! поток будет закрыт!</remarks>
        public static byte[] ReadBytesToEnd(Stream stream) {
            LinkedList<byte[]> blocks = new LinkedList<byte[]>();
            int count = 0;
            using (BinaryReader reader = new BinaryReader(stream)) {
                while (stream.CanRead) {
                    byte[] buff = reader.ReadBytes(BYTE_BLOCK_SIZE);
                    if (buff.Length == 0) {
                        break;
                    }
                    blocks.AddLast(buff);
                    count += buff.Length;
                }
            }
            byte[] res = new byte[count];
            int step = 0;
            foreach (byte[] block in blocks) {
                Array.Copy(block, 0, res, step, block.Length);
                step += block.Length;
            }
            return res;
        }
    }
}
