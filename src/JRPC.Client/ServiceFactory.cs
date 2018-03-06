using Castle.DynamicProxy;

namespace JRPC.Client {
    internal static class ServiceFactory {
        private static readonly ProxyGenerator _generator = new ProxyGenerator(new DefaultProxyBuilder(new ModuleScope(true)));

        /// <summary>
        /// Создает прокси объект с интерфейсом <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="intercepter"></param>
        /// <returns></returns>
        public static T CreateWithInterceptor<T>(IInterceptor intercepter) where T : class {
            return _generator.CreateInterfaceProxyWithoutTarget<T>(intercepter);
        }

    }
}
    