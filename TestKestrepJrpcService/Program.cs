using System;
using Consul;
using JRPC.Service;
using JRPC.Service.Configuration.Core;
using JRPC.Service.Host.Kestrel;
using JRPC.Service.Registry;

namespace TestKestrepJrpcService
{
    class Program
    {
        static void Main(string[] args)
        {
            var defaultModulesRegistry = new DefaultModulesRegistry();
            defaultModulesRegistry.AddJRpcModule(new NewTestService());
            var service = new JRpcService(defaultModulesRegistry, new ConsulClient(), new KestrelJRpcServerHost(), new JrpcConfigurationManager());
            service.Start();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
    

    public class NewTestService : JRpcModule
    {
        public string Test(int first, int second)
        {
            return (first + second).ToString();
        }
    }
}