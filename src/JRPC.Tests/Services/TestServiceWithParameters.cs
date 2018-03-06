using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using JRPC.Service;

namespace Tests.Services {
    public class TestServiceWithParameters : JRpcModule, ITestServiceWithParameters {
        public const string DEFAULT_PARAMETER_VALUE = "defaultParameterValue";
        public const string NEW_DEFAULT_PARAMETER_VALUE = "newDefaultParameterValue";

        public string MethodWithDefaulParameter(string par, string defaultParameter = DEFAULT_PARAMETER_VALUE) {
            return defaultParameter;
        }

        public string MethodWithParameters(string par1, string nonDefaultParameter) {
            return nonDefaultParameter;
        }

    }
}