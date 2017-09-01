using System;
using Newtonsoft.Json;

namespace JRPC.Core {
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcException : ApplicationException {

        private readonly string _stacktrace;
        
        public JRpcException(string message) {
            this.message = message;
        }

        [JsonConstructor]
        public JRpcException(string message, string stackTrace) {
            this.message = message;
            _stacktrace = stackTrace;
        }

        public JRpcException(Exception exception, string moduleInfo, string method) {
            var remoteException = exception as JRpcException;
            message = remoteException != null ? remoteException.message : exception.GetType().Name + ": " + exception.Message;
            var stackTrace = remoteException != null ? remoteException.stacktrace : exception.StackTrace;
            _stacktrace = stackTrace + $"\r\n\r\n<---- handled by {moduleInfo}, {method}";
        }

        public JRpcException(string message, string moduleInfo, string method) {
            this.message = message;
            _stacktrace = $"\r\n\r\n<---- handled by {moduleInfo}, {method}";
        }


        [JsonProperty]
        public string message { get; set; }


        [JsonProperty]
        public string stacktrace => _stacktrace + StackTrace;

        public override string ToString() {
            return $"{base.ToString()}, RpcExceptionMessage = {message}, RpcExceptionData = {stacktrace}";
        }
    }
}
