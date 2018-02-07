using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.Hosting;
using Owin;

namespace JRPC.Service.Host.Owin
{
    public class OwinJrpcServer : IJrpcServerHost
    {

        private IDisposable _server;

        public bool StartServerHost(string hostingUrl, Func<JrpcContext, Task> requestProcessor)
        {
            _server = WebApp.Start(new StartOptions(hostingUrl)
            {
                ServerFactory = typeof(OwinHttpListener).Namespace
            }, app => app.Run(async context => await requestProcessor(ConvertToContext(context))
            ));
            return true;
        }


        private JrpcContext ConvertToContext(IOwinContext context)
        {

            if (context == null)
            {
                return null;
            }

            var jrpcContext = new JrpcContext
            {
                JrpcRequestContext = new OwinJrpcRequestContext(context.Request),
                JrpcResponseContext = new OwinJrpcResponseContext(context.Response)
            };
            return jrpcContext;
        }

        


        public void Dispose()
        {
            _server.Dispose();
        }
    }
}