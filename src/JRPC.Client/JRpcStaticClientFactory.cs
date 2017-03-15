using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JRPC.Client {
    internal static class JRpcStaticClientFactory {
        private static readonly ConcurrentDictionary<Tuple<string, string, Type>, object> _proxiesCache = new ConcurrentDictionary<Tuple<string, string, Type>, object>();

        //NOTE: НЕ УДАЛЯТЬ, т.к. используется рефлексией
        private static object Invoke<TResult>(IJRpcClient client, string taskName, string methodName, string parametersStr, JsonSerializerSettings jsonSerializerSettings) {
            return client.Call(taskName, methodName, parametersStr)
                .AfterSuccess(r =>
                    JsonConvert.DeserializeObject<TResult>(r, jsonSerializerSettings));
        }

        public static T Get<T>(IJRpcClient client, string taskName, string cacheKey, JsonSerializerSettings jsonSerializerSettings) where T : class {
            return (T)_proxiesCache.GetOrAdd(Tuple.Create(cacheKey, taskName, typeof(T)),
                k => ServiceFactory.CreateWithInterceptor<T>(new JRpcIntercepter(client, k.Item2, jsonSerializerSettings)));
        }

        /// <summary>
        /// Выполняет функцию в синхронном continuation
        /// </summary>
        public static Task<TResult> AfterSuccess<TSrc, TResult>(this Task<TSrc> task, Func<TSrc, TResult> resultTransformer) {
            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith(t => {
                if (t.IsCanceled) {
                    tcs.SetCanceled();
                } else if (t.IsFaulted && t.Exception != null) {
                    tcs.SetException(t.Exception);
                } else {
                    try {
                        tcs.SetResult(resultTransformer(t.Result));
                    } catch (Exception e) {
                        tcs.SetException(e);
                    }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }
    }
}
