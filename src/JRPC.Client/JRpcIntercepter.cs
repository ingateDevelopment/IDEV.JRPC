using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Newtonsoft.Json;
using NLog;

namespace JRPC.Client {

    internal class JRpcIntercepter : IInterceptor {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly MethodInfo _invokeMethod = typeof(JRpcStaticClientFactory).GetMethod("Invoke", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly ConcurrentDictionary<MethodInfo, Tuple<Invoker, bool>> _invokers = new ConcurrentDictionary<MethodInfo, Tuple<Invoker, bool>>();
        private delegate object Invoker(IJRpcClient client, string taskName, string methodName, string parametersStr, JsonSerializerSettings jsonSerializerSettings);

        private readonly IJRpcClient _client;
        private readonly string _taskName;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JRpcIntercepter(IJRpcClient client, string taskName, JsonSerializerSettings jsonSerializerSettings) {
            _client = client;
            _taskName = taskName;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public void Intercept(IInvocation invocation) {
            var dictionary = new Dictionary<string, object>();
            var parameters = invocation.Method.GetParameters();
            for (int i = 0; i < parameters.Length; i++) {
                dictionary[parameters[i].Name] = invocation.Arguments[i];
            }
            var parametersStr = SerializeParams(dictionary);
            var invoker = _invokers.GetOrAdd(invocation.Method, GetInvoker);
            var result = invoker.Item1(_client, _taskName, invocation.Method.Name, parametersStr, _jsonSerializerSettings);
            bool needReturnTask = invoker.Item2;
                invocation.ReturnValue = result;
            if (needReturnTask) {
            } else {
                invocation.ReturnValue = (object)((dynamic)result).Result;
            }
        }
        private string SerializeParams(Dictionary<string, object> dictionary) {
            return JsonConvert.SerializeObject(dictionary, _jsonSerializerSettings);
        }

        private Tuple<Invoker, bool> GetInvoker(MethodInfo methodInfo) {
            var returnType = methodInfo.ReturnType;
            bool needReturnTask = false;
            if (returnType == typeof(void)) {
                returnType = typeof(object);
            } else if (returnType == typeof(Task)) {
                returnType = typeof(object);
                needReturnTask = true;
            } else if (typeof(Task).IsAssignableFrom(returnType)) {
                returnType = returnType.GetGenericArguments()[0];
                needReturnTask = true;
            }
            return Tuple.Create((Invoker)Delegate.CreateDelegate(typeof(Invoker), _invokeMethod.MakeGenericMethod(returnType)), needReturnTask);
        }
    }
}
