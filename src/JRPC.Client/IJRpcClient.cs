using System.Threading.Tasks;

namespace JRPC.Client {
    public interface IJRpcClient {
        Task<string> Call(string name, string method, string serializedParameters);
        Task<TResult> Call<TResult>(string name, string method, object parameters);
        T GetProxy<T>(string taskName) where T : class;
    }
}
