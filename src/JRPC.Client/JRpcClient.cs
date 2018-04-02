using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JRPC.Client.Extensions;
using JRPC.Core;
using JRPC.Core.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace JRPC.Client {

    public class JRpcClient : IJRpcClient {
        private const string METHOD = "POST";
        private const string DEFAULT_ADDRESS = "http://localhost:12345";

        private static TimeSpan DefaultTimeout => TimeSpan.FromHours(1.0);

        private static JsonSerializerSettings DefaultSettings =>
            new JsonSerializerSettings {ContractResolver = new DefaultContractResolver()};

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _endpoint;
        private readonly TimeSpan _timeout;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JRpcClient() : this(DEFAULT_ADDRESS) {
        }

        public JRpcClient(string endpoint) : this(endpoint, DefaultSettings) {
        }

        public JRpcClient(string endpoint, JsonSerializerSettings jsonSerializerSettings) : this(endpoint,
            DefaultTimeout, jsonSerializerSettings) {
            _endpoint = endpoint;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public JRpcClient(string endpoint, TimeSpan timeout, JsonSerializerSettings jsonSerializerSettings) {
            _endpoint = endpoint;
            _timeout = timeout;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public async Task<TResult> Call<TResult>(JrpcClientCallParams clientCallParams) {
            return await InvokeRequest<TResult>(clientCallParams).ConfigureAwait(false);
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

        private Lazy<string> _processName = new Lazy<string>(() => Process.GetCurrentProcess().ProcessName);
        private Lazy<string> _currentIp = new Lazy<string>(() => Array.FindLast(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork).ToString());

        private string ProcessName => _processName.Value;
        private string CurrentIp => _currentIp.Value;

        private async Task<T> InvokeRequest<T>(JrpcClientCallParams clientCallParams) {
            var id = Guid.NewGuid().ToString();

            var request = new JRpcRequest {
                Id = id,
                Method = clientCallParams.MethodName,    
                Params = clientCallParams.ParametersStr,
            };
            var serviceInfos = JrpcRegistredServices.GetAllInfo();
            var currentServiceInfo = serviceInfos.FirstOrDefault();

            var clientServiceName = currentServiceInfo.Key;
            var clientServiceProxyName = currentServiceInfo.Value.FirstOrDefault();
            _logger.Log(new LogEventInfo {
                Level = LogLevel.Trace,
                LoggerName = _logger.Name,
                Message = "Request for {0}.{1} with ID {2} sent.",
                Parameters = new object[] {clientCallParams.ServiceName, clientCallParams.MethodName, id},
                Properties = {
                    {"service", clientCallParams.ServiceName},
                    {"method", clientCallParams.MethodName},
                    {"requestId", id},
                    {"process", ProcessName},
                    {"currentIp", CurrentIp},
                    {"proxyTypeName", clientCallParams.ProxyType.FullName}, 
                    {"currentServiceName", clientServiceName},
                    {"currentProxyName", clientServiceProxyName}
                }
            });

            var jsonresponse = await HttpAsyncRequest<T>(METHOD, "application/json",
                GetEndPoint(clientCallParams.ServiceName), request,
                _timeout, clientCallParams.Credentials, clientCallParams.ProxyType, clientServiceName, clientServiceProxyName).ConfigureAwait(false);

            _logger.Log(new LogEventInfo {
                Level = LogLevel.Debug,
                LoggerName = _logger.Name,
                Message = "Response for {0}.{1} with ID {2} received.",
                Parameters = new[] { clientCallParams.ServiceName, clientCallParams.MethodName, jsonresponse.Id},
                Properties = {
                    {"service", clientCallParams.ServiceName},
                    {"method", clientCallParams.MethodName},
                    {"requestId", jsonresponse.Id},
                    {"process", ProcessName}, 
                    {"currentIp", CurrentIp },
                    {"proxyTypeName", clientCallParams.ProxyType.FullName },
                    {"status", jsonresponse.Error != null ? "fail" : "ok"},
                    {"source", "client"}, 
                    {"currentServiceName", clientServiceName},
                    {"currentProxyName", clientServiceProxyName}
                }
            });

            if (jsonresponse.Error != null) {
                throw jsonresponse.Error;
            }

            var result = jsonresponse.Result;
            if (result == null) {
                return default(T);
            }

            return result;
        }

        /// <summary>
        /// Аснихронное выполнение jrpc запроса
        /// </summary>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        /// <param name="url"></param>
        /// <param name="jRpcRequest"></param>
        /// <param name="timeout"></param>
        /// <param name="credentials"></param>
        /// <param name="proxyType"></param>
        /// <param name="clientServiceName"></param>
        /// <param name="clientServiceProxyName"></param>
        /// <returns></returns>
        private async Task<JRpcResponse<T>> HttpAsyncRequest<T>(string method, string contentType, string url,
            JRpcRequest jRpcRequest, TimeSpan timeout, IAbstractCredentials credentials, Type proxyType,
            string clientServiceName, string clientServiceProxyName) {
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

            request.Headers.Add(JRpcHeaders.CLIENT_IP_HEADER_NAME, CurrentIp);
            request.Headers.Add(JRpcHeaders.CLIENT_PROCESS_NAME_HEADER_NAME, ProcessName);
            request.Headers.Add(JRpcHeaders.CLIENT_PROXY_INTERFACE_NAME, proxyType.FullName);
            if (!string.IsNullOrEmpty(clientServiceName)) {
                request.Headers.Add(JRpcHeaders.CLIENT_SERVICE_NAME, clientServiceName);
            }

            if (!string.IsNullOrEmpty(clientServiceProxyName)) {
                request.Headers.Add(JRpcHeaders.CLIENT_SERVICE_PROXY_NAME, clientServiceProxyName);
            }



            var serializer = JsonSerializer.Create(_jsonSerializerSettings);

            if (jRpcRequest != null) {
                using (var streamWriter =
                    new JsonTextWriter(new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false)))) {
                    serializer.Serialize(streamWriter, jRpcRequest);
                    streamWriter.Flush();
                }
            }

            HttpWebResponse response;
            try {
                response = (HttpWebResponse) await request.GetResponseAsync()
                    .WithTimeout(request.Timeout)
                    .ConfigureAwait(false);
            }
            catch (WebException ex) {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    response = (HttpWebResponse) ex.Response;
                }
                else {
                    response = null;
                }
            }
            catch (TimeoutException) {
                _logger.Error("Timeout occurred during service invocation.");
                response = null;
            }

            var stream = response?.GetResponseStream();
            if (stream == null) {
                throw new Exception($"Response from {url} is empty.");
            }

            using (var sr = new StreamReader(stream)) {
                using (var jsonTextReader = new JsonTextReader(sr)) {
                    return serializer.Deserialize<JRpcResponse<T>>(jsonTextReader);
                }
            }
        }

        #region Упрощенное создание JRpcClient

        /// <summary>
        ///     Создает сервис для подключения
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceName"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(string serviceName, string address = DEFAULT_ADDRESS)
            where TService : class {
            return Create<TService>(serviceName, DefaultTimeout, DefaultSettings, address);
        }

        /// <summary>
        ///     Создает сервис для подключения
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceName"></param>
        /// <param name="timeout">таймаут</param>
        /// <param name="settings">настройки</param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(string serviceName, TimeSpan timeout, JsonSerializerSettings settings,
            string address = DEFAULT_ADDRESS) where TService : class {

            var client = new JRpcClient(address, timeout, settings);
            return client.GetProxy<TService>(serviceName);
        }

        /// <summary>
        ///     Создает сервис для подключения. Имя сервиса будет TService(и отрежет I букву вначале если с нее начинается)
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(string address = DEFAULT_ADDRESS) where TService : class {
            return Create<TService>(DefaultTimeout, DefaultSettings, address);
        }

        /// <summary>
        ///     Создает сервис для подключения. Имя сервиса будет TService(и отрежет I букву вначале если с нее начинается)
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(TimeSpan timeout, string address = DEFAULT_ADDRESS)
            where TService : class {
            return Create<TService>(timeout, DefaultSettings, address);
        }

        /// <summary>
        ///     Создает сервис для подключения. Имя сервиса будет TService(и отрежет I букву вначале если с нее начинается)
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="settings"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(JsonSerializerSettings settings, string address = DEFAULT_ADDRESS)
            where TService : class {
            return Create<TService>(DefaultTimeout, settings, address);
        }

        /// <summary>
        ///     Создает сервис для подключения. Имя сервиса будет TService(и отрежет I букву вначале если с нее начинается)
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="timeout">таймаут</param>
        /// <param name="settings">настройки</param>
        /// <param name="address">адрес</param>
        /// <returns></returns>
        public static TService Create<TService>(TimeSpan timeout, JsonSerializerSettings settings,
            string address = DEFAULT_ADDRESS) where TService : class {
            const string PREFIX_TO_START_TRIM = "I";

            var name = typeof(TService).Name;
            if (name.StartsWith(PREFIX_TO_START_TRIM))
                name = name.Substring(1);
            return Create<TService>(name, timeout, settings, address);
        }

        #endregion

    }
}