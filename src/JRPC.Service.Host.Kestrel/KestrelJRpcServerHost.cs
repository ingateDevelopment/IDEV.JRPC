using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Owin;

namespace JRPC.Service.Host.Kestrel {
    public class KestrelJRpcServerHost : IJrpcServerHost {
        private IWebHost _host;

        public void Dispose() {
            _host.StopAsync().Wait();
            _host?.Dispose();
        }


        public bool StartServerHost(string hostingUrl, Func<JrpcContext, Task> requestProcessor) {
            _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(hostingUrl)
                .Configure(app => {
                    app.UseOwin(pipeline => {
                        pipeline(next => (objects => ProcessRequest(objects, requestProcessor)));
                    });
                })
                .Build();
            _host.RunAsync();
            return true;
        }

        private Task ProcessRequest(IDictionary<string, object> environment, Func<JrpcContext, Task> requestProcessor) {
            var context = ConvertToContext(environment);
            return requestProcessor(context);
        }


        private JrpcContext ConvertToContext(IDictionary<string, object> environment) {
            OwinFeatureCollection collection = new OwinFeatureCollection(environment);

            var context = new JrpcContext {
                JrpcRequestContext = new KestrelJrpcRequestContext(collection, collection),
                JrpcResponseContext = new KestrelJrpcResponseContext(collection)
            };
            return context;
        }


    }
}
