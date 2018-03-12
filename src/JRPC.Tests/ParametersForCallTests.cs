using System.Net.Sockets;
using JRPC.Core;
using NUnit.Framework;
using Tests.Services;

namespace Tests {
    [TestFixture]
    public class ParametersForCallTests {

        private const string TestData = "testString";

        [Test]
        public void PassValueToOptionalParameterTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParameters>("TestServiceWithParameters", new TestServiceWithParameters());
            var service = startInfo.Item1;
            service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt", TestData);
            Assert.AreEqual(TestData, methodWithDefaulParameter);
            service.Stop();

        }

        [Test]
        public void MissValueToOptionalParameterTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParameters>("TestServiceWithParameters", new TestServiceWithParameters(), port: "11119");
            var service = startInfo.Item1;
            service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt");
            Assert.AreEqual(TestServiceWithParameters.DEFAULT_PARAMETER_VALUE, methodWithDefaulParameter);
            service.Stop();
        }


        [Test]
        public void MissValueToOptionalParameterForClientWithOtherDefaultTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersAndNewDefaultValue>("TestServiceWithParameters", new TestServiceWithParameters(), port: "11118");
            var service = startInfo.Item1;
            service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt");
            Assert.AreEqual(TestServiceWithParameters.NEW_DEFAULT_PARAMETER_VALUE, methodWithDefaulParameter);
            service.Stop();
        }

        [Test]
        public void MissValueToOptionalParameterForOldClientVersionTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersOldVersion>("TestServiceWithParameters", new TestServiceWithParameters(), port: "11117");
            var service = startInfo.Item1;
            service.Start();
            Assert.AreEqual(TestServiceWithParameters.DEFAULT_PARAMETER_VALUE, startInfo.Item2.MethodWithDefaulParameter("smt"));
            service.Stop();
        }


        [Test]
        public void MissValueToNonOptionalParameterForOldClientVersionTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersOldVersion>("TestServiceWithParameters", new TestServiceWithParameters(), port: "11116");
            var service = startInfo.Item1;
            service.Start();
            Assert.Throws<JRpcException>(() => startInfo.Item2.MethodWithParameters("smt"));
            service.Stop();
        }


        [Test]
        public void CallMethodWithSchemeMismatchTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParamsNameMismatch>("TestServiceWithParamsNameMismatch", new TestServiceWithParamsNameMismatch(), port: "11115");
            var service = startInfo.Item1;
            service.Start();
            var paramValue = "test";
            Assert.AreEqual(paramValue, startInfo.Item2.Method(paramValue));
            service.Stop();
        }

        [Test]
        public void CallMethodOnServiceWithMultipleInterfaces() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParamsNameMismatch2>("TestServiceWithParamsNameMismatch", new TestServiceWithParamsNameMismatch(), port: "11114");
            var service = startInfo.Item1;
            service.Start();
            var paramValue = "test";
            Assert.AreEqual(paramValue, startInfo.Item2.AnotherMethod(paramValue));
            service.Stop();
        }


        [Test]
        public void CallIntersectInterfaceMethod() {
            Assert.Throws<JRpcException>(
                () =>
                    ServiceRunner.StartService<ITestServiceWithInterfaceMethodIntersectAndParameterNameMismatch>("TestServiceWithInterfaceMethodIntersect",
                        new TestServiceWithInterfaceMethodIntersect(), port: "11113"));
        }

        [Test]
        public void CallIntersectInterfaceMethod2() {
            Assert.Throws<JRpcException>(
                () =>
                    ServiceRunner.StartService<ITestServiceWithInterfaceMethodIntersect>("TestServiceWithInterfaceMethodIntersect2",
                        new TestServiceWithInterfaceMethodIntersect2(), port: "11111"));
        }

        [Test]
        public void CallMethodWithInterfaceInheritance() {
            var startInfo = ServiceRunner.StartService<ITestServiceImpl>("TestServiceImpl", new TestServiceImpl(), port: "11112");
            var service = startInfo.Item1;
            service.Start();

            Assert.AreEqual(TestService.STRING, startInfo.Item2.GetString());
            service.Stop();
        }
    }
}