using System.Collections.Generic;
using System.Threading.Tasks;
using JRPC.Core.Security;

namespace JRPC.Client {

    public interface IJRpcClient {

        Task<TResult> Call<TResult>(string name, string method, Dictionary<string, object> parameters, IAbstractCredentials credentials);
        T GetProxy<T>(string taskName) where T : class;

    }

}