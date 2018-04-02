using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace JRPC.Core {
    /// <summary>
    /// Содержит информацию о сервисах внутри процесса 
    /// </summary>
    public static class JrpcRegistredServices {

        private static ConcurrentDictionary<string, HashSet<string>> _registredServiceInfo = new ConcurrentDictionary<string, HashSet<string>>(); 
            

        public static bool AddService(string serviceName, HashSet<string> proxiesName) {
            return _registredServiceInfo.TryAdd(serviceName, proxiesName);
        }

        public static List<string> GetProxies() {
            return _registredServiceInfo.Values.SelectMany(t => t).ToList();
        }

        public static List<string> GetServices() {
            return _registredServiceInfo.Keys.ToList();
        }

        public static Dictionary<string, HashSet<string>> GetAllInfo() {
            return _registredServiceInfo.ToDictionary(s => s.Key, s => s.Value);
        }

        public static HashSet<string> GetProxiesForService(string serviceName) {
            _registredServiceInfo.TryGetValue(serviceName, out var result);
            return result;
        }

    }
}