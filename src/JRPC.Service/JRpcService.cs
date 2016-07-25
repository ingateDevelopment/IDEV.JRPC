using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Consul;
using Microsoft.Owin.Hosting;
using NLog;
using Owin;
using JRPC.Service.Regestry;

namespace JRPC.Service {

    public sealed class JRpcService {
        private const int DEFAULT_START_PORT = 5678;
        private const int DEFAULT_END_PORT = 60000;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IModulesRegestry _modulesRegestry;
        private readonly IConsulClient _consulClient;
        private readonly List<string> _registeredConsulIds = new List<string>();
        private IDisposable _server;

        public JRpcService(IModulesRegestry modulesRegestry, IConsulClient consulClient) {
            _modulesRegestry = modulesRegestry;
            _consulClient = consulClient;
        }

        public bool Start() {
            var address = GetAddress();

            var services = _modulesRegestry.GetAllServices();
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
                foreach (var p in GetAvailiablePorts()) {
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

            string url = "http://" + address + ":" + port + "/";
            _logger.Info("Starting RPC service on {0}...", url);


            foreach (var service in services.Keys) {
                _registeredConsulIds.Add(RegisterInConsul(service, url, address, port.Value));
            }
            _logger.Info("Зарегистрировали в консуле {0} сервисов: {1}", _registeredConsulIds.Count, string.Join(", ", _registeredConsulIds));
            return true;
        }

        private bool StartServices(Dictionary<string, JRpcModule> services, string address, int port) {
            string url = "http://" + address + ":" + port + "/";
            try {
                _server = WebApp.Start(new StartOptions(url) {
                    ServerFactory = typeof(Microsoft.Owin.Host.HttpListener.OwinHttpListener).Namespace
                }, app => app.Run(context => {
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

                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return context.Response.WriteAsync(string.Empty);
                }));
                return true;
            } catch (TargetInvocationException e) {
                if (e.InnerException is HttpListenerException) {
                    _logger.Warn(string.Format("Unable start service on port {0}", port), e);
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
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

            List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList();

            var unusedPorts = Enumerable.Range(DEFAULT_START_PORT, DEFAULT_END_PORT - DEFAULT_START_PORT)
                .Where(port => !usedPorts.Contains(port)).ToArray();
            return unusedPorts.OrderBy(t => Guid.NewGuid()).ToList();
        }

        public bool Stop() {
            _server.Dispose();
            foreach (var serviceId in _registeredConsulIds) {
                _consulClient.Agent.ServiceDeregister(serviceId);
            }
            return true;
        }

        private string RegisterInConsul(string moduleName, string baseUrl, string address, int port) {
            var consulServiceId = String.Format("{0}:{1}-{2}", address, port, moduleName);
            _consulClient.Agent.ServiceRegister(new AgentServiceRegistration {
                Name = moduleName,
                Address = address,
                Port = port,
                ID = consulServiceId,
                Tags = new[] { "urlprefix-/" + moduleName },
                Check = new AgentServiceCheck {
                    HTTP = baseUrl + moduleName,
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(10),
                }
            });

            return consulServiceId;
        }

        private static string GetAddress() {
            var configValue = ConfigurationManager.AppSettings.Get("ServiceAddress");
            //if (!string.IsNullOrWhiteSpace(configValue)) {
                return configValue;
            //} 
        }
    }
}
