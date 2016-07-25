using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRPC.Core {
    [JsonObject(MemberSerialization.OptIn)]
    public class JRpcRequest {
        private string _jsonrpc = "2.0";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JToken Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("jsonrpc")]
        public string Jsonrpc {
            get { return _jsonrpc; }
            set { _jsonrpc = value; }
        }
    }
}
