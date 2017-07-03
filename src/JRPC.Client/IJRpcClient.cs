using System.Threading.Tasks;
using JRPC.Core.Security;

namespace JRPC.Client {

    public interface IJRpcClient {

        Task<string> Call(string name, string method, string serializedParameters, IAbstractCredentials credentials);
        Task<TResult> Call<TResult>(string name, string method, object parameters, IAbstractCredentials credentials);
        T GetProxy<T>(string taskName) where T : class;

    }

}