using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using NLog;

namespace JRPC.Client {

    public class JRpcClient : IJRpcClient {

        private const string Method = "POST";

        private static Logger _logger = LogManager.GetCurrentClassLogger();
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

        public Task<TResult> Call<TResult>(string name, string method, Dictionary<string, object> parameters, IAbstractCredentials credentials) {
            return Task.FromResult(InvokeRequest<TResult>(name, method, parameters, credentials));
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

        private T InvokeRequest<T>(string url, string method, object data, IAbstractCredentials credentials) {
            var respData = InvokeRequest(url, method, data, credentials);
            if (respData == null) {
                return default(T);
            }
            var tmp = JToken.FromObject(respData);
            return tmp.ToObject<T>();
        }

        private object InvokeRequest(string service, string method, object data, IAbstractCredentials credentials) {
            var id = Guid.NewGuid().ToString();

            var request = new JRpcRequest {
                Id = id,
                Method = method,
                Params = data,
            };

            _logger.Log(new LogEventInfo {
                Level = LogLevel.Debug,
                LoggerName = _logger.Name,
                Message = "Request for {0}.{1} with ID {2} sent.",
                Parameters = new object[] {service, method, id},
                Properties = {{"service", service}, {"method", method}, {"RequestID", id}, {"Process", Process.GetCurrentProcess().ProcessName}}
            });

            var jsonresponse = HttpAsyncRequest(Method,
                "application/json",
                GetEndPoint(service),
                request,
                _timeout, credentials).Result;

            _logger.Log(new LogEventInfo {
                Level = LogLevel.Debug,
                LoggerName = _logger.Name,
                Message = "Response for {0}.{1} with ID {2} received.",
                Parameters = new object[] {service, method, jsonresponse.Id},
                Properties = {{"service", service}, {"method", method}, {"RequestID", jsonresponse.Id}, {"Process", Process.GetCurrentProcess().ProcessName}}
            });

            if (jsonresponse.Error != null) {
                throw jsonresponse.Error;
            }

            return jsonresponse.Result;
        }

        /// <summary>
        /// Копипаста AsyncTools HttpAsyncRequester.Request
        /// </summary>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        /// <param name="url"></param>
        /// <param name="jRpcRequest"></param>
        /// <param name="timeout"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private async Task<JRpcResponse> HttpAsyncRequest(string method, string contentType, string url,
            JRpcRequest jRpcRequest, TimeSpan timeout, IAbstractCredentials credentials) {
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

            var serializer = JsonSerializer.Create(_jsonSerializerSettings);

            if (jRpcRequest != null) {
                using (var streamWriter = new JsonTextWriter(new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false)))) {
                    serializer.Serialize(streamWriter, jRpcRequest);
                    streamWriter.Flush();
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
                _logger.Error("Timeout occurred during service invocation.", ex);
                response = null;
            }
            var stream = response?.GetResponseStream();
            if (stream == null) {
                throw new Exception($"Response from {url} is empty.");
            }

            using (var sr = new StreamReader(stream)) {
                using (var jsonTextReader = new JsonTextReader(sr)) {
                    return serializer.Deserialize<JRpcResponse>(jsonTextReader);
                }
            }
        }

    }

}