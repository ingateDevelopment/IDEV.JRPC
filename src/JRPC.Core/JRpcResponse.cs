using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace JRPC.Core {
    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcResponse {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonProperty(PropertyName = "result")]
        public object Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public JRpcException Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcResponse<T> {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonProperty(PropertyName = "result")]
        public T Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public JRpcException Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }
    }
}
