using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Consul;
using Microsoft.Owin.Hosting;
using NLog;
using Owin;
using JRPC.Service.Registry;
using Prometheus;
using Prometheus.Owin;

namespace JRPC.Service {

    public sealed class JRpcService {

        private const int DefaultStartPort = 5678;
        private const int DefaultEndPort = 60000;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Gauge _counter = Metrics.CreateGauge("requests", "help text", labelNames: new[] {"module"});
        
        private readonly IModulesRegistry _modulesRegistry;
        private readonly IConsulClient _consulClient;
        private readonly List<string> _registeredConsulIds = new List<string>();
        private IDisposable _server;

        public JRpcService(IModulesRegistry modulesRegistry, IConsulClient consulClient) {
            _modulesRegistry = modulesRegistry;
            _consulClient = consulClient;
        }

        public bool Start() {
            var address = GetAddress();
            return Start(address);
        }

        public bool Start(string address) {
            var services = _modulesRegistry.GetAllServices();
            int? port = GetPort();
            List<int> availiablePorts = GetAvailiablePorts();

            if (port.HasValue) {
                if (!availiablePorts.Contains(port.Value)) {
                    _logger.Fatal("Port {0} allready in use", port);
                    return false;
                }

                if (!StartServices(services, address, port.Value)) {
                    _logger.Fatal("Unable start service on port {0}", port);
                    return false;
                }
            } else {
                foreach (var p in availiablePorts) {
                    if (StartServices(services, address, p)) {
                        port = p;
                        break;
                    }
                }
                if (!port.HasValue) {
                    _logger.Fatal("Unable start service on any port");
                    return false;
                }
            }

            string url = $"http://{address}:{port}/";
            _logger.Info("Starting RPC service on {0}...", url);

            foreach (var service in services.Keys) {
                _registeredConsulIds.Add(RegisterInConsul(service, url, address, port.Value));
                services[service].BindingUrl = $"{url}{service}";

            }
            _logger.Info("Зарегистрировали в консуле {0} сервисов: {1}", _registeredConsulIds.Count, string.Join(", ", _registeredConsulIds));
            return true;
        }

        private bool StartServices(Dictionary<string, JRpcModule> services, string address, int port) {
            string url = "http://" + address + ":" + port + "/";
            try {
                _server = WebApp.Start(new StartOptions(url) {
                    ServerFactory = typeof(Microsoft.Owin.Host.HttpListener.OwinHttpListener).Namespace,
                }, app => {
                    app.UsePrometheusServer();
                    app.Run(context => {
                        if (context.Request.Path.HasValue) {
                            var path = context.Request.Path.Value.TrimStart('/');
                            JRpcModule module;
                            if (services.TryGetValue(path, out module)) {
                                if (context.Request.Method == "POST") {
                                    return module.ProcessRequest(context);
                                }
                                return module.PrintInfo(context);
                            }
                        }

                        context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                        return context.Response.WriteAsync(string.Empty);
                    });
                });
                return true;
            } catch (TargetInvocationException e) {
                if (e.InnerException is HttpListenerException) {
                    _logger.Warn($"Unable start service on port {port}", e);
                } else {
                    throw;
                }
            }

            return false;
        }

        private static int? GetPort() {
            var configValue = ConfigurationManager.AppSettings.Get("ServicePort");
            if (!string.IsNullOrWhiteSpace(configValue)) {
                return Convert.ToInt32(configValue);
            }
            return null;
        }

        private static List<int> GetAvailiablePorts() {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();
            var usedPorts = tcpEndPoints.Select(p => p.Port).ToList();
            var unusedPorts = Enumerable
                .Range(DefaultStartPort, DefaultEndPort - DefaultStartPort)
                .Where(port => !usedPorts.Contains(port)).ToArray();
            return unusedPorts.OrderBy(t => Guid.NewGuid()).ToList();
        }

        public bool Stop() {
            _server?.Dispose();
            foreach (var serviceId in _registeredConsulIds) {
                _consulClient.Agent.ServiceDeregister(serviceId);
            }
            return true;
        }

        private string RegisterInConsul(string moduleName, string baseUrl, string address, int port) {
            var consulServiceId = $"{address}:{port}-{moduleName}";
            _consulClient.Agent.ServiceRegister(new AgentServiceRegistration {
                Name = moduleName,
                Address = address,
                Port = port,
                ID = consulServiceId,
                Tags = new[] {"urlprefix-/" + moduleName},
                Check = new AgentServiceCheck {
                    HTTP = baseUrl + moduleName,
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(10),
                    DeregisterCriticalServiceAfter = TimeSpan.FromHours(1),
                }
            });

            return consulServiceId;
        }

        private static string GetAddress() {
            var configValue = ConfigurationManager.AppSettings.Get("ServiceAddress");
            return !string.IsNullOrWhiteSpace(configValue) ? configValue : GetAdressFromDns();
        }

        private static string GetAdressFromDns() {
            return Array.FindLast(
                Dns.GetHostEntry(string.Empty).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

    }

}