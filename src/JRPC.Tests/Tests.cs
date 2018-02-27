using System;
using System.Threading.Tasks;
using JRPC.Core;
using JRPC.Service;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Tests;
using Tests.Services;

namespace JRPC.Tests {

    [TestFixture]
    public class Tests {
        private JRpcService _service;
        private ITestService _client;

        [SetUp]
        public void StartService() {
            var startInfo = ServiceRunner.StartService<ITestService>("TestService", new TestService());
            _service = startInfo.Item1;
            _client = startInfo.Item2;
        }

        [TearDown]
        public void StopService() {
            _service.Stop();
            _client = null;
        }

        [Test]
        public void TestGetInt() {
            Assert.AreEqual(TestService.ONE, _client.GetInt());
        }

        [Test]
        public void TestGetString() {
            string actual = string.Empty;
            try {
                actual = _client.GetString();
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
            
            
            
            
            Assert.AreEqual(TestService.STRING, actual);
        }
        
        [Test]
        public void TestPerformLongTask() {
            var veryLongTaskResult = _client.VeryLongTask("info");
            Assert.AreNotEqual(veryLongTaskResult.Status, TaskStatus.RanToCompletion);
            var result = veryLongTaskResult.Result;
            Assert.AreEqual(veryLongTaskResult.Status, TaskStatus.RanToCompletion);
        }

        [Test]
        public void TestGetList() {
            var actual = _client.GetList();
            Assert.AreEqual(TestService.LIST, actual);
            Assert.AreNotSame(TestService.LIST, actual);
        }

        [Test]
        public void TestThrowException() {
            Assert.Throws<JRpcException>(() => _client.ThrowException());
        }

        [Test]
        public void TestGetTask() {
            Assert.AreEqual(TestService.STRING, _client.GetTask().Result);
        }

        [Test]
        public void TestPerformLongTask() {
            var veryLongTaskResult = _client.VeryLongTask("info");
            Assert.AreNotEqual(veryLongTaskResult.Status, TaskStatus.RanToCompletion);
            var result = veryLongTaskResult.Result;
            Assert.AreEqual(veryLongTaskResult.Status, TaskStatus.RanToCompletion);
        }

        [Test]
        public void TestGetNull() {
            Assert.AreEqual(null, _client.GetNull());
        }

        [Test]
        public void TestGetDate() {
            Assert.AreEqual(TestService.DATE, _client.GetDate());
        }

        [Test]
        public void TestGetArray() {
            Assert.AreEqual(TestService.LIST.ToArray(), _client.GetArray());
        }

        [Test]
        public void TestGetDictionary() {
            var dictionary = _client.GetDict();
            Assert.AreEqual(TestService.DICT, dictionary);
        }

        [Test]
        public void TestPascalDictionary() {
            var dictionary = _client.GetPascalDict();
            Assert.AreEqual(TestService.PASCAL_DICT, dictionary);
        }

        [Test]
        public void TestCamelDictionary() {
            var dictionary = _client.GetCamelDict();
            Assert.AreEqual(TestService.CAMEL_DICT, dictionary);
        }

        [Test]
        public void TestLowerDictionary() {
            var dictionary = _client.GetLowerDict();
            Assert.AreEqual(TestService.LOWER_DICT, dictionary);
        }

        [Test]
        public void TestUpperDictionary() {
            var dictionary = _client.GetUpperDict();
            Assert.AreEqual(TestService.UPPER_DICT, dictionary);
        }

        [Test]
        public void TestGetDto() {
            var testDto = _client.GetDto();
            Assert.AreNotSame(TestService.DTO, testDto);
            Assert.AreEqual(TestService.DTO.IntField, testDto.IntField);
            Assert.AreEqual(TestService.DTO.intField, testDto.intField);
            Assert.AreEqual(TestService.DTO.String, testDto.String);
            Assert.AreEqual(TestService.DTO.StringArray, testDto.StringArray);
        }


        [Test]
        public void TestMethodWithOverridingByParams() {
            Assert.Throws<JRpcException>(
                () => ServiceRunner.StartService<ITestServiceImplWithOverridingByParams>("TestServiceImplWithOverridingByParams", new TestServiceImplWithOverridingByParams()));
        }
    }
}