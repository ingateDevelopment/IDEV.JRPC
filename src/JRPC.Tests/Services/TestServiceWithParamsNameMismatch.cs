using JRPC.Service;

namespace Tests.Services {
    public class TestServiceWithParamsNameMismatch : JRpcModule, ITestServiceWithParamsNameMismatch, ITestServiceWithParamsNameMismatch2 {

        public string Method(string par) {
            return par;
        }

        public string MethodWihInterfecaIntersect(string par) {
            return par;
        }


        public string AnotherMethod(string par) {
            return par;
        }
    }

    public class TestServiceWithInterfaceMethodIntersect : JRpcModule, ITestServiceWithParamsNameMismatch, ITestServiceWithInterfaceMethodIntersectAndParameterNameMismatch {
        public string Method(string par1) {
            return par1;
        }

        public string MethodWihInterfecaIntersect(string par) {
            return par;
        }

    }

    public class TestServiceWithInterfaceMethodIntersect2 : JRpcModule, ITestServiceWithParamsNameMismatch, ITestServiceWithInterfaceMethodIntersect {
        public string Method(string par1) {
            return par1;
        }

        public string MethodWihInterfecaIntersect(string par) {
            return par;
        }

    }
}