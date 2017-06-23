using JRPC.Service;
using NUnit.Framework;
using Tests.Services;

namespace Tests {
    [TestFixture]
    public class DeriviedClassTests {
        private JRpcService _service;
        private ITestServiceImpl _client;

        [SetUp]
        public void StartService() {
            var startInfo = ServiceRunner.StartService<ITestServiceImpl>("TestServiceImpl", new TestServiceImpl());
            _service = startInfo.Item1;
            _client = startInfo.Item2;
        }

        [TearDown]
        public void StopService() {
            _service.Stop();
            _client = null;
        }


        [Test]
        public void TestBaseGetString() {
            Assert.AreEqual(TestService.STRING, _client.GetString());
        }

        [Test]
        public void TestVirualMethodWithouOverriding() {
            Assert.AreEqual(TestService.BASE_STRING, _client.GetVirtualString());

        }

        [Test]
        public void TestVirtualMethodWithOverriding() {
            var serviceInfo = ServiceRunner.StartService<ITestServiceImpl>("TestServiceImplWithOverriding", new TestServiceImplWithOverriding(), port: "9999");
            var client = serviceInfo.Item2;
            Assert.AreEqual(TestServiceImplWithOverriding.IMPL_STRING, client.GetOverrideString());
            serviceInfo.Item1.Stop();
            client = null;
        }

        [Test]
        public void TestMethodWithNew() {
            var serviceInfo = ServiceRunner.StartService<ITestServiceImpl>("TestServiceImplWithOverriding", new TestServiceImplWithOverriding(), port: "9999");
            var client = serviceInfo.Item2;
            Assert.AreEqual(TestServiceImplWithOverriding.IMPL_STRING, client.GetOverrideString());
            serviceInfo.Item1.Stop();
            client = null;
        }
    }
}