using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using JRPC.Core;

namespace JRPC.Service {

    public abstract class JRpcModule {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, MethodInvoker> _handlers = new Dictionary<string, MethodInvoker>();
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public virtual string ModuleName { get { return GetType().Name; } }

        private readonly DateTime _buildTime;

        public Func<IOwinContext, Task> PrintInfo {
            get {
                return (context) => Task.FromResult(string.Format("Module [{0}] built at {1}", ModuleName, _buildTime.ToString("yyyy-MM-dd HH:mm:ss")));
            }
        }

        public Func<IOwinContext, Task> ProcessRequest {
            get {
                return (context) => {
                    var reader = new StreamReader(context.Request.Body);
                    var content = reader.ReadToEnd();
                    var request = JsonConvert.DeserializeObject<JRpcRequest>(content, _jsonSerializerSettings);
                    if (_logger.IsTraceEnabled) {
                        _logger.Trace("Processing request. Service [{0}]. Body {1}", ModuleName, content);
                    }

                    MethodInvoker handle;

                    var haveDelegate = _handlers.TryGetValue(request.Method.ToLower(), out handle);
                    if (!haveDelegate || handle == null) {
                        var response = new JRpcResponse {
                            Result = null,
                            Error = new JRpcException(-32601, "Method not found", "The method does not exist / is not available."),
                            Id = request.Id
                        };
                        return SerializeResponse(context.Response, response);
                    }
                    try {
                        var resp = new JRpcResponse {
                            Id = request.Id,
                            Result = handle.Invoke(this, request.Params)
                        };
                        return SerializeResponse(context.Response, resp);
                    } catch (Exception ex) {
                        var response = new JRpcResponse {
                            Result = null,
                            Error = new JRpcException(-32602, "Method execution exception", ex.ToString()),
                            Id = request.Id
                        };
                        return SerializeResponse(context.Response, response);
                    }
                };
            }
        }

        protected virtual IList<JsonConverter> JsonConverters { get { return new JsonConverter[0]; } }

        protected JRpcModule() {
            _jsonSerializerSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = JsonConverters,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            BuildService();

            _buildTime = GetLinkerTime(GetType().Assembly);
        }

        private Task SerializeResponse(IOwinResponse response, JRpcResponse rpcResponse) {
            var str = JsonConvert.SerializeObject(rpcResponse, _jsonSerializerSettings);
            response.ContentType = "application/json";
            return response.WriteAsync(str);
        }

        private void BuildService() {
            var type = GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var serialiser = JsonSerializer.Create(_jsonSerializerSettings);
            foreach (var method in methods) {
                var attribute = method.GetCustomAttributes(typeof(JRpcMethodAttribute), false).SingleOrDefault() as JRpcMethodAttribute;
                var methodName = attribute != null && !string.IsNullOrWhiteSpace(attribute.MethodName)
                    ? attribute.MethodName.ToLower()
                    : method.Name.ToLower();

                _handlers.Add(methodName, new MethodInvoker(method, serialiser));
            }
        }

        private static DateTime GetLinkerTime(Assembly assembly) {
            var filePath = assembly.Location;
            const int C_PE_HEADER_OFFSET = 60;
            const int C_LINKER_TIMESTAMP_OFFSET = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                stream.Read(buffer, 0, 2048);
            }

            var offset = BitConverter.ToInt32(buffer, C_PE_HEADER_OFFSET);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + C_LINKER_TIMESTAMP_OFFSET);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            return TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, TimeZoneInfo.Local);
        }
    }
}
