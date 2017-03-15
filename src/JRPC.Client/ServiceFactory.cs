using System.ServiceModel;
using System.ServiceModel.Channels;
using Castle.DynamicProxy;

namespace JRPC.Client {
    internal static class ServiceFactory {
        private static readonly ProxyGenerator _generator = new ProxyGenerator(new PersistentProxyBuilder());

        /// <summary>
        /// Создает прокси объект с интерфейсом <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binding"></param>
        /// <param name="endpointAddress"></param>
        /// <returns></returns>
        public static T Create<T>(Binding binding, EndpointAddress endpointAddress) where T : class {
            var intercepter = new ServiceProxyIntercepter<T>(binding, endpointAddress);
            return _generator.CreateInterfaceProxyWithoutTarget<T>(intercepter);
        }

        public static T CreateWithInterceptor<T>(IInterceptor intercepter) where T : class {
            return _generator.CreateInterfaceProxyWithoutTarget<T>(intercepter);
        }

    }
}
