using System;
using Consul;
using JRPC.Service;
using JRPC.Service.Host.Kestrel;
using JRPC.Service.Host.Owin;
using JRPC.Service.Registry;

namespace TestKestrepJrpcService
{
    class Program
    {
        static void Main(string[] args)
        {
            var defaultModulesRegistry = new DefaultModulesRegistry();
            defaultModulesRegistry.AddJRpcModule(new NewTestService());
            var service = new JRpcService(defaultModulesRegistry, new ConsulClient(), new KestrelJRpcServerHost());
            service.Start();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }


    public interface INewTestService {
        string Test(int first, int second);
    }

    public class NewTestService : JRpcModule, INewTestService {
        public string Test(int first, int second)
        {
            return (first + second).ToString();
        }
    }
}