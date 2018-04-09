using System;
using System.Collections.Generic;
using JRPC.Core.Security;
using Newtonsoft.Json;

namespace JRPC.Client {
    public class JrpcClientCallParams {
        public string ServiceName{ get; set; }
        public  string MethodName{ get; set; }
        public Dictionary<string, object> ParametersStr{ get; set; }
        public JsonSerializerSettings JsonSerializerSettings{ get; set; }
        public IAbstractCredentials Credentials{ get; set; }
        public Type ProxyType{ get; set; }
    }
}