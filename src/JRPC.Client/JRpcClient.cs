using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        //NOTE: если никто не меняет состояние у класса, то может имеет смысл создать один экземпляр static и везде его юзать?
        private static TimeSpan DefaultTimeout => TimeSpan.FromHours(1.0);

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _endpoint;
        private readonly TimeSpan _timeout;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JRpcClient() : this(DEFAULT_ADDRESS) { }

        public JRpcClient(string endpoint) : this(endpoint,
            new JsonSerializerSettings {ContractResolver = new DefaultContractResolver()}) { }

        public JRpcClient(string endpoint, JsonSerializerSettings jsonSerializerSettings) : this(endpoint, DefaultTimeout, jsonSerializerSettings) {
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

        private T InvokeRequest<T>(string service, string method, object data, IAbstractCredentials credentials) {
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

            var jsonresponse = HttpAsyncRequest<T>(METHOD, "application/json", GetEndPoint(service), request, _timeout,
                credentials).Result;

            _logger.Log(new LogEventInfo {
                Level = LogLevel.Debug,
                LoggerName = _logger.Name,
                Message = "Response for {0}.{1} with ID {2} received.",
                Parameters = new[] {service, method, jsonresponse.Id},
                Properties = {{"service", service}, {"method", method}, {"RequestID", jsonresponse.Id}, {"Process", Process.GetCurrentProcess().ProcessName}}
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
        /// Копипаста AsyncTools HttpAsyncRequester.Request
        /// </summary>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        /// <param name="url"></param>
        /// <param name="jRpcRequest"></param>
        /// <param name="timeout"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private async Task<JRpcResponse<T>> HttpAsyncRequest<T>(string method, string contentType, string url,
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
            } catch (WebException ex) {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    response = (HttpWebResponse) ex.Response;
                } else {
                    response = null;
                }
            } catch (TimeoutException) {
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
        
        //NOTE: если никто не меняет состояние у класса, то может имеет смысл создать один экземпляр static и везде его юзать?
        private static JsonSerializerSettings DefaultSettings => new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        ///     Создает сервис для подключения
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceName"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(string serviceName, string address = DEFAULT_ADDRESS) where TService : class {
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
        public static TService Create<TService>(TimeSpan timeout, string address = DEFAULT_ADDRESS) where TService : class {
            return Create<TService>(timeout, DefaultSettings, address);
        }

        /// <summary>
        ///     Создает сервис для подключения. Имя сервиса будет TService(и отрежет I букву вначале если с нее начинается)
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="settings"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static TService Create<TService>(JsonSerializerSettings settings, string address = DEFAULT_ADDRESS) where TService : class {
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