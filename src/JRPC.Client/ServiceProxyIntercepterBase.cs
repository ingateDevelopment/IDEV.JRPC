using System;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using Castle.DynamicProxy;
using NLog;

namespace JRPC.Client {
    internal abstract class ServiceProxyIntercepterBase<TChannel> : IInterceptor where TChannel : class {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const int RETRY_DELAY = 1;

        protected ChannelFactory<TChannel> _factory;
        private readonly Binding _binding;
        private readonly EndpointAddress _endpointAddress;

        public ServiceProxyIntercepterBase(Binding binding, EndpointAddress endpointAddress) {
            _binding = binding;
            _endpointAddress = endpointAddress;
            InitFactory();
        }

        private void InitFactory() {
            _factory = new ChannelFactory<TChannel>(_binding, _endpointAddress);
            foreach (OperationDescription op in _factory.Endpoint.Contract.Operations) {
                var dataContractBehavior =
                    op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (dataContractBehavior != null) {
                    dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                }
            }
        }

        public void Intercept(IInvocation invocation) {
            TChannel proxy = null;
            int countExec = 0;
            Exception lastException = null;
            while (countExec < RetryCount) {
                try {
                    proxy = GetConnection();
                    invocation.ReturnValue = invocation.Method.Invoke(proxy, invocation.Arguments);
                    ReleaseConnection(proxy);
                    lastException = null;
                    break;
                } catch (TimeoutException e) {
                    lastException = e;
                    countExec++;
                    ReInitProxy(proxy);
                    Thread.Sleep(countExec * RETRY_DELAY * 1000);
                } catch (CommunicationException e) {
                    lastException = e;
                    countExec++;
                    ReInitProxy(proxy);
                    Thread.Sleep(countExec * RETRY_DELAY * 1000);
                } catch (TargetInvocationException e) {
                    if (e.InnerException is TimeoutException || e.InnerException is CommunicationException
                        || e.InnerException is SocketException) {
                        lastException = e.InnerException;
                        countExec++;
                        ReInitProxy(proxy);
                        Thread.Sleep(countExec * RETRY_DELAY * 1000);
                    }
                }
            }
            if (lastException != null) {
                throw lastException;
            }
        }

        public abstract TChannel GetConnection();
        public abstract void ReleaseConnection(TChannel proxy);
        protected abstract int RetryCount { get; }

        private void ReInitProxy(TChannel proxy) {
            if (proxy != null) {
                ((IClientChannel)proxy).Abort();
            }
            if (_factory.State != CommunicationState.Opened) {
                _factory.Close();
                InitFactory();
            }
        }
    }

    internal class ServiceProxyIntercepter<TChannel> : ServiceProxyIntercepterBase<TChannel> where TChannel : class {
        private const int RETRY_COUNT = 10;
        public ServiceProxyIntercepter(Binding binding, EndpointAddress endpointAddress) : base(binding, endpointAddress) { }

        public override TChannel GetConnection() {
            TChannel proxy = _factory.CreateChannel();
            ((IClientChannel)proxy).Open();
            return proxy;
        }

        public override void ReleaseConnection(TChannel proxy) {
            ((IClientChannel)proxy).Close();
        }

        protected override int RetryCount {
            get {
                return RETRY_COUNT;
            }
        }
    }
}
