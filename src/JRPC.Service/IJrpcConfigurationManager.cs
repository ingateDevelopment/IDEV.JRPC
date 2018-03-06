namespace JRPC.Service {
    public interface IJrpcConfigurationManager {
        string Get(string key);
        object GetSection(string sectionName);
        string GetConnectionString(string connectionName);
    }
}