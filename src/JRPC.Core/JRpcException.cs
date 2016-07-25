using System;
using Newtonsoft.Json;

namespace JRPC.Core {
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcException : ApplicationException {
        public JRpcException(int code, string message, object data) {
            this.code = code;
            this.message = message;
            this.data = data;
        }

        [JsonProperty]
        public int code { get; set; }

        [JsonProperty]
        public string message { get; set; }

        [JsonProperty]
        public object data { get; set; }

        public override string ToString() {
            string dataStr = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            return string.Format("{0}, RpcExceptionMessage = {1}, RpcExceptionData = {2}", base.ToString(), message, dataStr);
        }
    }
}
