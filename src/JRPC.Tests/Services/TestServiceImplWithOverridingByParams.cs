using JRPC.Service;

namespace Tests.Services {
    public class TestServiceImplWithOverridingByParams : JRpcModule, ITestServiceImplWithOverridingByParams {
        public string Method(string par) {
            return "Method with one argument";
        }

        public string Method(string par1, string par2) {
            return "Method with tow arguments";
        }
    }
}