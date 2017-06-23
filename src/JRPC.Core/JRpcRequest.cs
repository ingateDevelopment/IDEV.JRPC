using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JRPC.Core {
    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcRequest {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JToken Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";
    }
}
