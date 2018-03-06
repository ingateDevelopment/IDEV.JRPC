using System;
using Consul;
using JRPC.Registry.Ninject;
using JRPC.Service;
using JRPC.Service.Configuration.Net45;
using JRPC.Service.Host.Owin;
using JRPC.Service.Registry;
using Ninject.Modules;
using Topshelf;
using Topshelf.Ninject;

namespace TestJrpcOwinService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(c => {
                c.UseNinject(new StatisticCommonServiceNinjectModule())
                    .Service<JRpcService>(s => {
                        s.ConstructUsingNinject();
                        s.WhenStarted((service, control) => service.Start());
                        s.WhenStopped((service, control) => service.Stop());
                    });
                c.RunAsNetworkService();
                c.SetServiceName("StatisticCommonService");
                c.SetDisplayName("StatisticCommonService");
                c.SetDescription("StatisticCommonService for Ingate");
            });
            Console.ReadLine();
        }
        
        
        
    }
    public class StatisticCommonServiceNinjectModule : NinjectModule {
        public override void Load()
        {
            Bind<IJrpcServerHost>().To<OwinJrpcServer>().InSingletonScope();
            Bind<IConsulClient>().To<ConsulClient>();
            Bind<IJrpcConfigurationManager>().To<JrpcConfigurationManagerNet45>();
            Bind<JRpcModule>().To<NewTestService>();
            Bind<IModulesRegistry>().To<NinjectModulesRegistry>();
            
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