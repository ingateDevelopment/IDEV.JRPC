# IDEV.JRPC #

**IDEV.JRPC** is JSON-RPC library based on [JSON-RPC.NET](https://github.com/Astn/JSON-RPC.NET). It's provide server and client parts.

## Requirements

* dotnet 4.5.1
* Nlog >= 3.1.0
* ConsulClient >= 0.7.2
* Ninject >= 3.2.2 (optional: only for **JRPC.Registry.Ninject**)
* Microsoft.Owin.Host.HttpListener >= 3.0.1

Also [Consul](https://www.consul.io) agent needed to work properly.

## Install ##

To install IDEV.JRPC Service, run the following command in the Package Manager Console:
```
PM> Install-Package JRPC.Service
```

To install IDEV.JRPC Client, run the following command in the Package Manager Console:
```
PM> Install-Package JRPC.Client
```

To install IDEV.JRPC Ninject Registry, run the following command in the Package Manager Console:
```
PM> Install-Package JRPC.Registry.Ninject
```

## Usage ##

Add `appSettings` section in App.config:
```XML
<configuration>
...
<appSettings>
<add key="ServicePort" value="12345" />
<add key="ServiceAddress" value="127.0.0.1" />
</appSettings>
...
</configuration>
```

#### Example Service:

```csharp
public interface ISomeService {
    string GetString();
}
 
public class SomeService : JRpcModule, ISomeService {  
    public string GetString() {
        return "Result from service";
    }
}
```

#### Basic Usage:

```csharp
var consulClient = new ConsulClient();
var registry = new DefaultModulesRegistry();
registry.AddJRpcModule(new SomeService());
var svc = new JRpcService(registry, consulClient);
svc.Start();
Console.ReadLine();
svc.Stop();
```

#### With Ninject:

Define Ninject module like this:

```csharp
public class SomeNinjectModule : NinjectModule {
    public override void Load() {
        Bind<JRpcModule>().To<SomeService>();
        Bind<IModulesRegistry>().To<NinjectModulesRegistry>();
        Bind<IConsulClient>().To<ConsulClient>();
        Bind<JRpcService>().ToSelf();
    }
}
```

```csharp
var kernel = new StandardKernel();
kernel.Load<SomeNinjectModule>();
var svc = kernel.Get<JRpcService>();
svc.Start();
Console.ReadLine();
svc.Stop();
```

#### Client example:

```csharp
var client = new JRpcClient("http://127.0.0.1:12345");
var proxy = client.GetProxy<ISomeService>("SomeService");
Console.WriteLine(proxy.GetString());   // Output is "Result from service"
```

See [full example solution](https://github.com/ingateDevelopment/IDEV.JRPC.Example)

## License ##

IDEV.JRPC is licensed under [The MIT License](https://opensource.org/licenses/MIT).