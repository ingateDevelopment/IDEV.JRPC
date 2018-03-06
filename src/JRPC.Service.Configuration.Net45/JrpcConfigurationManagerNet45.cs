using System.Configuration;

namespace JRPC.Service.Configuration.Net45 {
    public class JrpcConfigurationManagerNet45 : IJrpcConfigurationManager {
        public string Get(string key) {
            return ConfigurationManager.AppSettings.Get(key);
        }

        public object GetSection(string sectionName) {
            return ConfigurationManager.AppSettings.Get(sectionName);
        }

        public string GetConnectionString(string connectionName) {
            return ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }
    }
}