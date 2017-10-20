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
using Newtonsoft.Json.Linq;

namespace JRPC.Service {

    public abstract class JRpcModule {

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, MethodInvoker> _handlers = new Dictionary<string, MethodInvoker>();

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public virtual string ModuleName => GetType().Name;

        public string BindingUrl { get; set; }

        private readonly DateTime _buildTime;
        private JsonSerializer _jsonSerializer;

        public Func<IOwinContext, Task> PrintInfo {
            get { return (context) => Task.FromResult(ModuleInfo); }
        }

        public string ModuleInfo => $"Module [{ModuleName}] built at {_buildTime:yyyy-MM-dd HH:mm:ss} bindingUrl at {BindingUrl}";

        public Func<IOwinContext, Task> ProcessRequest {
            get {
                return (context) => {
                    JRpcRequest request = null;
                    using (var reader = new JsonTextReader(new StreamReader(context.Request.Body))) {
                        request = _jsonSerializer.Deserialize<JRpcRequest>(reader);
                    }

                    if (request == null) {
                        var newEx = new JRpcException("Empty JSONRPC request.");
                        var response = new JRpcResponse {
                            Result = null,
                            Error = newEx
                        };
                        _logger.Error("Error occurred during method invocation.", newEx);
                        SerializeResponse(context.Response, response);
                        return Task.FromResult(0);
                    }

                    _logger.Log(new LogEventInfo {
                        Level = LogLevel.Debug,
                        LoggerName = _logger.Name,
                        Message = "Request for {0}.{1} with ID {2} received.",
                        Parameters = new object[] {ModuleName, request.Method, request.Id},
                        Properties = {{"service", ModuleName}, {"method", request.Method}, {"RequestID", request.Id}}
                    });

                    if (_logger.IsTraceEnabled) {
                        _logger.Trace("Processing request. Service [{0}]. Method {1}", ModuleName, request.Method);
                    }

                    MethodInvoker handle;

                    var methodName = request.Method.ToLower();
                    var haveDelegate = _handlers.TryGetValue(methodName, out handle);
                    if (!haveDelegate || handle == null) {
                        var response = new JRpcResponse {
                            Result = null,
                            Error = new JRpcException("Method not found. The method does not exist / is not available.", ModuleInfo, methodName),
                            Id = request.Id
                        };
                        SerializeResponse(context.Response, response);
                    } else {
                        try {
                            var resp = new JRpcResponse {
                                Id = request.Id,
                                Result = handle.Invoke(this, request.Params as JToken)
                            };
                            _logger.Log(new LogEventInfo {
                                Level = LogLevel.Debug,
                                LoggerName = _logger.Name,
                                Message = "Response by {0}.{1} with ID {2} sent.",
                                Parameters = new object[] {ModuleName, request.Method, request.Id},
                                Properties = {{"service", ModuleName}, {"method", request.Method}, {"RequestID", request.Id}}
                            });

                            SerializeResponse(context.Response, resp);
                        } catch (Exception ex) {
                            while (ex is AggregateException){
                                ex = (ex as AggregateException).InnerException;
                            }
                            var newEx = new JRpcException(ex, ModuleInfo, methodName);
                            var response = new JRpcResponse {
                                Result = null,
                                Error = newEx,
                                Id = request.Id
                            };
                            _logger.Error("Error occurred during method invocation.", newEx);
                            SerializeResponse(context.Response, response);
                        }
                    }
                    return Task.FromResult(0);
                };
            }
        }

        protected virtual IList<JsonConverter> JsonConverters => new JsonConverter[0];

        protected virtual JsonSerializerSettings GetSerializerSettings() {
            return new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = JsonConverters,
                ContractResolver = new DefaultContractResolver()
            };
        }

        protected JRpcModule() {
            _jsonSerializerSettings = GetSerializerSettings();
            _jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
            BuildService();
            _buildTime = GetLinkerTime(GetType().Assembly);
        }

        private void SerializeResponse(IOwinResponse response, JRpcResponse rpcResponse) {
            response.ContentType = "application/json";
            using (var jw = new JsonTextWriter(new StreamWriter(response.Body))) {
                var serializer = JsonSerializer.Create(_jsonSerializerSettings);
                serializer.Serialize(jw, rpcResponse);
            }
        }

        private void BuildService() {
            var type = GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderByDescending(t => t.DeclaringType == type);
            var interfaces = type.GetInterfaces();

            var serialiser = JsonSerializer.Create(_jsonSerializerSettings);

            var duplicateMethod = methods.GroupBy(t => Tuple.Create(t.Name, t.DeclaringType)).FirstOrDefault(t => t.Count() > 1);
            if (duplicateMethod != null) {
                var methodInfo = duplicateMethod.ToList().First();
                throw new JRpcException($"Method with name {methodInfo.Name} already exist in type {type}", ModuleInfo, methodInfo.Name);
            }
            var methodInfos = interfaces.SelectMany(
                    i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance)).ToList()
                .GroupBy(t => t.Name.ToLower()).ToList();

            var duplicateInterfaceMethod = methodInfos.FirstOrDefault(t => t.Count() > 1);
            if (duplicateInterfaceMethod != null) {
                var methodInfo = duplicateInterfaceMethod.ToList().First();
                throw new JRpcException($"Method with name {methodInfo.Name} already exist in interfaces {type}", ModuleInfo, methodInfo.Name);
            }

            var interfaceMethodsMap = methodInfos.ToDictionary(t => t.Key, t => t.OrderByDescending(s => s.DeclaringType == type).FirstOrDefault());

            foreach (var method in methods.OrderByDescending(t => t.DeclaringType == type)) {
                var attribute = method.GetCustomAttributes(typeof(JRpcMethodAttribute), false).SingleOrDefault() as JRpcMethodAttribute;
                var methodName = !string.IsNullOrWhiteSpace(attribute?.MethodName)
                    ? attribute.MethodName.ToLower()
                    : method.Name.ToLower();

                if (_handlers.ContainsKey(methodName) && method.DeclaringType != type) {
                    continue;
                }
                MethodInfo interfaceMethodInfo = null;
                interfaceMethodsMap.TryGetValue(methodName, out interfaceMethodInfo);
                var methodInfo = interfaceMethodInfo ?? method;
                _handlers.Add(methodName, new MethodInvoker(methodInfo, serialiser));
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