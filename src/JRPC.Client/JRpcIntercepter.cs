using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using JRPC.Core.Security;
using Newtonsoft.Json;

namespace JRPC.Client {

    internal class JRpcIntercepter : IInterceptor {

        private static readonly MethodInfo _invokeMethod = typeof(JRpcStaticClientFactory).GetMethod("Invoke", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly IJRpcClient _client;

        private readonly ConcurrentDictionary<MethodInfo, Tuple<Invoker, bool>> _invokers =
            new ConcurrentDictionary<MethodInfo, Tuple<Invoker, bool>>();

        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly string _taskName;

        private readonly IAbstractCredentials _credentials;

        public JRpcIntercepter(IJRpcClient client, string taskName, JsonSerializerSettings jsonSerializerSettings) : this(client, taskName, jsonSerializerSettings, null) {
        }

        public JRpcIntercepter(IJRpcClient client, string taskName, JsonSerializerSettings jsonSerializerSettings, IAbstractCredentials credentials) {
            _client = client;
            _taskName = taskName;
            _jsonSerializerSettings = jsonSerializerSettings;
            _credentials = credentials;
        }

        public void Intercept(IInvocation invocation) {
            var dictionary = new Dictionary<string, object>();
            var parameters = invocation.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++) {
                dictionary[parameters[i].Name] = invocation.Arguments[i];
            }
            var parametersStr = SerializeParams(dictionary);
            var invoker = _invokers.GetOrAdd(invocation.Method, GetInvoker);
            try {
                var result = invoker.Item1(_client, _taskName, invocation.Method.Name, parametersStr,
                    _jsonSerializerSettings, _credentials);

                var needReturnTask = invoker.Item2;
                invocation.ReturnValue = result;
                if (!needReturnTask) {
                    invocation.ReturnValue = (object)((dynamic)result).Result;
                }
            } catch (AggregateException e) {
                Exception ex = e;
                while (ex is AggregateException) {
                    ex = (ex as AggregateException).InnerException;
                }
                throw ex;
            }
        }

        private string SerializeParams(Dictionary<string, object> dictionary) {
            return JsonConvert.SerializeObject(dictionary, _jsonSerializerSettings);
        }

        private Tuple<Invoker, bool> GetInvoker(MethodInfo methodInfo) {
            var returnType = methodInfo.ReturnType;
            var needReturnTask = false;
            if (returnType == typeof(void)) {
                returnType = typeof(object);
            } else if (returnType == typeof(Task)) {
                returnType = typeof(object);
                needReturnTask = true;
            } else if (typeof(Task).IsAssignableFrom(returnType)) {
                returnType = returnType.GetGenericArguments()[0];
                needReturnTask = true;
            }
            return
                Tuple.Create(
                    (Invoker)Delegate.CreateDelegate(typeof(Invoker), _invokeMethod.MakeGenericMethod(returnType)),
                    needReturnTask);
        }

        private delegate object Invoker(
            IJRpcClient client,
            string taskName,
            string methodName,
            string parametersStr,
            JsonSerializerSettings jsonSerializerSettings,
            IAbstractCredentials credentials
            );

    }

}