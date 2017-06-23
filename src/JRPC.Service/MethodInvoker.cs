using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JRPC.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JRPC.Service {
    internal class MethodInvoker {
        private readonly JsonSerializer _jsonSerializer;
        private readonly Func<object, JToken, object> _delegate;
        private readonly ParameterInfo[] _parameters;

        public MethodInvoker(MethodInfo methodInfo, JsonSerializer jsonSerializer) {
            _jsonSerializer = jsonSerializer;
            _parameters = methodInfo.GetParameters();

            var instance = Expression.Parameter(typeof(object), "instance");
            var jToken = Expression.Parameter(typeof(JToken), "jToken");

            _delegate = Expression.Lambda<Func<object, JToken, object>>(CreateCall(instance, jToken, methodInfo), instance, jToken).Compile();
        }

        public object Invoke(object instance, JToken parameters) {
            var res = _delegate(instance, parameters);
            var task = res as Task;
            if (task != null) {
                return ((dynamic)task).Result;
            }
            return res;
        }

        private static T GetArg<T>(JToken j, int i, IReadOnlyList<ParameterInfo> parameters, JsonSerializer jsonSerializer) {
            var jObj = j as JObject;
            JToken value;
            ParameterInfo parameterInfo;
            if (jObj != null) {
                parameterInfo = parameters[i];
                value = jObj[parameterInfo.Name];
            } else {
                var jArr = (JArray)j;
                parameterInfo = parameters[i];
                value = jArr[i];
            }
            if (value != null) {
                return value.ToObject<T>(jsonSerializer);
            }

            if (!parameterInfo.IsOptional) {
                throw new JRpcException($"Not found expectedparams with name {parameterInfo.Name}", Environment.StackTrace);
            }

            if (parameterInfo.HasDefaultValue) {
                return (T)parameterInfo.DefaultValue;
            }
            return default(T);
        }

        private Expression CreateCall(Expression instance, Expression jToken, MethodInfo methodInfo) {
            var getArg = typeof(MethodInvoker).GetMethod("GetArg", BindingFlags.NonPublic | BindingFlags.Static);
            var paramsExpressions = _parameters.Select((p, i) => {
                var getArgTyped = getArg.MakeGenericMethod(p.ParameterType);
                return Expression.Call(getArgTyped, jToken, Expression.Constant(i), Expression.Constant(_parameters), Expression.Constant(_jsonSerializer));
            });
            var callExpression = Expression.Call(Expression.Convert(instance, methodInfo.ReflectedType), methodInfo, paramsExpressions);
            if (methodInfo.ReturnType == typeof(void)) {
                return Expression.Block(callExpression, Expression.Constant(null));
            }
            return Expression.Convert(callExpression, typeof(object));
        }
    }
}
