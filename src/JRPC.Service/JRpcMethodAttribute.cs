using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRPC.Service {
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class JRpcMethodAttribute : Attribute {
        readonly string _methodName;
        public JRpcMethodAttribute(string methodName = "") {
            _methodName = methodName;
        }

        public string MethodName {
            get { return _methodName; }
        }
    }
}
