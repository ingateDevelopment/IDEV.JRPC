using System;
using Consul;
using JRPC.Client;
using JRPC.Registry.Ninject;
using JRPC.Service;
using JRPC.Service.Host.Owin;
using JRPC.Service.Registry;
using Ninject.Modules;
using Topshelf;
using Topshelf.Ninject;

namespace TestJrpcOwinService
{
    public class Program
    {
        public static void Main(string[] args) {

            var client = new JRpcClient();


            var proxy = client.GetProxy<INewTestService>("NewTestService");


            proxy.Test(100, 50);

            Console.ReadLine();

//            
//            HostFactory.Run(c => {
//                c.UseNinject(new StatisticCommonServiceNinjectModule())
//                    .Service<JRpcService>(s => {
//                        s.ConstructUsingNinject();
//                        s.WhenStarted((service, control) => service.Start());
//                        s.WhenStopped((service, control) => service.Stop());
//                    });
//                c.RunAsNetworkService();
//                c.SetServiceName("StatisticCommonService");
//                c.SetDisplayName("StatisticCommonService");
//                c.SetDescription("StatisticCommonService for Ingate");
//            });
//            Console.ReadLine();
        }
        
        
        
    }
    public class StatisticCommonServiceNinjectModule : NinjectModule {
        public override void Load()
        {
            Bind<IJrpcServerHost>().To<OwinJrpcServer>().InSingletonScope();
            Bind<IConsulClient>().To<ConsulClient>();
            Bind<JRpcModule>().To<NewTestService>();
            Bind<IModulesRegistry>().To<NinjectModulesRegistry>();
            
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