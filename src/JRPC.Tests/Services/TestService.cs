using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JRPC.Service;
using Tests.Dto;

namespace Tests.Services
{
    public class TestService : JRpcModule, ITestService
    {
        internal static readonly DateTime DATE = new DateTime(100000);
        internal static readonly List<string> LIST = new List<string> {STRING};
        internal static readonly Dictionary<string, int> DICT = new Dictionary<string, int> {{STRING, ONE}};
        internal static readonly Dictionary<string, int> PASCAL_DICT = new Dictionary<string, int> {{"TestTestTest", ONE}};
        internal static readonly Dictionary<string, int> CAMEL_DICT = new Dictionary<string, int> {{"testTestTest", ONE}};
        internal static readonly Dictionary<string, int> LOWER_DICT = new Dictionary<string, int> {{"testtesttest", ONE}};
        internal static readonly Dictionary<string, int> UPPER_DICT = new Dictionary<string, int> {{"TESTTESTTEST", ONE}};
        internal static TestDto DTO = new TestDto { IntField = ONE, intField = ONE + ONE, String = STRING, StringArray = new[] {STRING}};
        internal const int ONE = 1;
        internal const string STRING = "Test";
        internal const string BASE_STRING = "BaseString";
        


        public int GetInt()
        {
            return ONE;
        }

        public string GetString()
        {
            return STRING;
        }

        public void ThrowException()
        {
            throw new NotImplementedException("this method not implemented yet");

            //throw new JRpcException(ONE, STRING, STRING);
        }

        public List<string> GetList()
        {
            return LIST;
        }
        
        public async Task<string> VeryLongTask(string source) {
            await Task.Delay(1000);
            return source;
        }

        public Task<string> GetTask()
        {
            return Task.FromResult(STRING);
        }

//        public async Task<string> VeryLongTask(string source) {
//            await Task.Delay(1000);
//            return source;
//        }

        public object GetNull()
        {
            return null;
        }

        public DateTime GetDate()
        {
            return DATE;
        }

        public string[] GetArray()
        {
            return LIST.ToArray();
        }

        public Dictionary<string, int> GetDict()
        {
            return DICT;
        }

        public TestDto GetDto()
        {
            return DTO;
        }

        public Dictionary<string, int> GetPascalDict()
        {
            return PASCAL_DICT;
        }

        public Dictionary<string, int> GetCamelDict()
        {
            return CAMEL_DICT;
        }

        public Dictionary<string, int> GetLowerDict()
        {
            return LOWER_DICT;
        }

        public Dictionary<string, int> GetUpperDict()
        {
            return UPPER_DICT;
        }

        public virtual string GetVirtualString() {
            return BASE_STRING;
        }

        public string GetOverrideString() {
            return BASE_STRING;
        }
    }
}