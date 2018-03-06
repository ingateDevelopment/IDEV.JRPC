using System;
using JRPC.Core;
using JRPC.Service;
using NUnit.Framework;
using Tests.Services;

namespace Tests {
    [TestFixture]
    public class ParametersForCallTests {

        private const string TestData = "testString";

        JRpcService _service;


        [TearDown]
        public void Stop() {
            if (_service != null) {
                _service.Stop();
            }
        }

        [Test]
        public void PassValueToOptionalParameterTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParameters>("TestServiceWithParameters", new TestServiceWithParameters());
            _service = startInfo.Item1;
            _service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt", TestData);
            Assert.AreEqual(TestData, methodWithDefaulParameter);

        }

        [Test]
        public void MissValueToOptionalParameterTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParameters>("TestServiceWithParameters", new TestServiceWithParameters());
            _service = startInfo.Item1;
            _service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt");
            Assert.AreEqual(TestServiceWithParameters.DEFAULT_PARAMETER_VALUE, methodWithDefaulParameter);
        }


        [Test]
        public void MissValueToOptionalParameterForClientWithOtherDefaultTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersAndNewDefaultValue>("TestServiceWithParameters", new TestServiceWithParameters());
            _service = startInfo.Item1;
            _service.Start();
            var methodWithDefaulParameter = startInfo.Item2.MethodWithDefaulParameter("smt");
            Assert.AreEqual(TestServiceWithParameters.NEW_DEFAULT_PARAMETER_VALUE, methodWithDefaulParameter);
        }

        [Test]
        public void MissValueToOptionalParameterForOldClientVersionTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersOldVersion>("TestServiceWithParameters", new TestServiceWithParameters());
            _service = startInfo.Item1;
            _service.Start();
            Assert.AreEqual(TestServiceWithParameters.DEFAULT_PARAMETER_VALUE, startInfo.Item2.MethodWithDefaulParameter("smt"));
        }


        [Test]
        public void MissValueToNonOptionalParameterForOldClientVersionTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParametersOldVersion>("TestServiceWithParameters", new TestServiceWithParameters());
            _service = startInfo.Item1;
            _service.Start();
            Assert.Throws<JRpcException>(() => startInfo.Item2.MethodWithParameters("smt"));
            
        }


        [Test]
        public void CallMethodWithSchemeMismatchTest() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParamsNameMismatch>("TestServiceWithParamsNameMismatch", new TestServiceWithParamsNameMismatch());
            _service = startInfo.Item1;
            _service.Start();
            var paramValue = "test";
            Assert.AreEqual(paramValue, startInfo.Item2.Method(paramValue));
        }

        [Test]
        public void CallMethodOnServiceWithMultipleInterfaces() {
            var startInfo = ServiceRunner.StartService<ITestServiceWithParamsNameMismatch2>("TestServiceWithParamsNameMismatch", new TestServiceWithParamsNameMismatch());
            _service = startInfo.Item1;
            _service.Start();
            var paramValue = "test";
            Assert.AreEqual(paramValue, startInfo.Item2.AnotherMethod(paramValue));
        }


        [Test]
        public void CallIntersectInterfaceMethod() {
            Assert.Throws<JRpcException>(
                () =>
                    ServiceRunner.StartService<ITestServiceWithInterfaceMethodIntersectAndParameterNameMismatch>("TestServiceWithInterfaceMethodIntersect",
                        new TestServiceWithInterfaceMethodIntersect()));
        }

        [Test]
        public void CallIntersectInterfaceMethod2() {
            Assert.Throws<JRpcException>(
                () =>
                    ServiceRunner.StartService<ITestServiceWithInterfaceMethodIntersect>("TestServiceWithInterfaceMethodIntersect2",
                        new TestServiceWithInterfaceMethodIntersect2()));
        }

        [Test]
        public void CallMethodWithInterfaceInheritance() {
            var startInfo = ServiceRunner.StartService<ITestServiceImpl>("TestServiceImpl", new TestServiceImpl());
            _service = startInfo.Item1;
            _service.Start();

            Assert.AreEqual(TestService.STRING, startInfo.Item2.GetString());

        }
    }
}