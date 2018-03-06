namespace JRPC.Service
{
    public class JrpcContext
    {
        public IJrpcRequestContext JrpcRequestContext { get; set; }

        public IJrpcResponseContext JrpcResponseContext { get; set; }
    }
}