﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using JRPC.Core.Security;
using Newtonsoft.Json;

namespace JRPC.Client {
    internal class JRpcIntercepter : IInterceptor {

        private static readonly MethodInfo _invokeMethod = typeof(JRpcStaticClientFactory).GetMethod("Invoke", BindingFlags.Static | BindingFlags.Public);

        private readonly IJRpcClient _client;

        private readonly ConcurrentDictionary<string, InterceptedMethod> _invokers =
            new ConcurrentDictionary<string, InterceptedMethod>();

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
            var invoker = _invokers.GetOrAdd(invocation.Method.Name.ToLowerInvariant(), GetInvoker(invocation.Method));
            try {
                var result = invoker.MethodInvoker(_client,
                    new JrpcClientCallParams {
                        ServiceName = _taskName,
                        MethodName =invocation.Method.Name.ToLowerInvariant(),
                        ParametersStr = dictionary,
                        JsonSerializerSettings = _jsonSerializerSettings,
                        Credentials = _credentials,
                        ProxyType = invocation.Method.DeclaringType
                    });
                var needReturnTask = invoker.NeedReturnTask;
                invocation.ReturnValue = needReturnTask ? result : (object) ((dynamic) result).Result;
            } catch (AggregateException e) {
                Exception ex = e;
                while (ex is AggregateException) {
                    ex = (ex as AggregateException).InnerException;
                }
                throw ex;
            }
        }

        private static InterceptedMethod GetInvoker(MethodInfo methodInfo) {
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
            return new InterceptedMethod() {
                MethodInvoker = (Invoker) Delegate.CreateDelegate(typeof(Invoker), _invokeMethod.MakeGenericMethod(returnType)),
                NeedReturnTask = needReturnTask
            };
        }

        private class InterceptedMethod {

            public Invoker MethodInvoker { get; set; }
            public bool NeedReturnTask { get; set; }

        }

        private delegate object Invoker(
            IJRpcClient client,
            JrpcClientCallParams clientCallParams
        );

    }

}