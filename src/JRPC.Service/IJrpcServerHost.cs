using System;
using System.Threading.Tasks;

namespace JRPC.Service {
    public interface IJrpcServerHost : IDisposable {
        bool StartServerHost(string hostingUrl, Func<JrpcContext, Task> requestProcessor);
    }
}