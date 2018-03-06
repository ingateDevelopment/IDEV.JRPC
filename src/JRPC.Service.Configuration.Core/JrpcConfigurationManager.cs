using System.Configuration;

namespace JRPC.Service.Configuration.Core {
    public class JrpcConfigurationManager : IJrpcConfigurationManager {
        public string Get(string key) {
            return ConfigurationManager.AppSettings.Get(key);
        }

        public object GetSection(string sectionName) {
            return ConfigurationManager.GetSection(sectionName);
        }

        public string GetConnectionString(string connectionName) {
            return ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }
    }
}