using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Owin;

namespace JRPC.Service.Host.Kestrel
{
    public class KestrelJRpcServerHost : IJrpcServerHost
    {
        private IWebHost host;

        public void Dispose()
        {
            host.StopAsync().Wait();
            host?.Dispose();
        }
        

        public bool StartServerHost(string hostingUrl, Func<JrpcContext, Task> requestProcessor)
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(hostingUrl)
                .Configure(app => { app.UseOwin(pipeline => { pipeline(next => (objects => ProcessRequest(objects, requestProcessor))); }); })
                .Build();
            host.Run();
            return true;
        }

        private Task ProcessRequest(IDictionary<string, object> environment, Func<JrpcContext, Task> requestProcessor)
        {
            var context = ConvertToContext(environment);
            return requestProcessor(context);
        }


        private JrpcContext ConvertToContext(IDictionary<string, object> environment)
        {
            OwinFeatureCollection collection = new OwinFeatureCollection(environment);

            var context = new JrpcContext
            {
                JrpcRequestContext = new KestrelJrpcRequestContext(collection),
                JrpcResponseContext = new KestrelJrpcResponseContext(collection)
            };
            return context;
        }

  
    }

//    public class Startup
//    {
//        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
//        public void ConfigureServices(IServiceCollection services)
//        {
//        }
//
//        public void Configure(IApplicationBuilder app)
//        {
//            app.UseOwin(pipeline =>
//            {
//                pipeline(next => (objects => OwinHello(objects)));
//            });
//        }
//
//        public Task OwinHello(IDictionary<string, object> environment)
//        {
//            
//            OwinFeatureCollection collection = new OwinFeatureCollection(environment);
//            
//            
//            
//            
//            string responseText = "Hello World via OWIN";
//            byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);
//
//            // OWIN Environment Keys: http://owin.org/spec/spec/owin-1.0.0.html
//            var responseStream = (Stream)environment["owin.ResponseBody"];
//            var responseHeaders = (IDictionary<string, string[]>)environment["owin.ResponseHeaders"];
//
//            responseHeaders["Content-Length"] = new string[] { responseBytes.Length.ToString(CultureInfo.InvariantCulture) };
//            responseHeaders["Content-Type"] = new string[] { "text/plain" };
//
//            return responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
//        }
//    }
}
