using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Dto;

namespace Tests.Services {
    public interface ITestService {
        int GetInt();
        string GetString();
        void ThrowException();
        List<string> GetList();
        Task<string> GetTask();
//        Task<string> VeryLongTask(string source);
        object GetNull();
        Task<string> VeryLongTask(string source);
        DateTime GetDate();
        string[] GetArray();
        Dictionary<string, int> GetDict();
        TestDto GetDto();
        Dictionary<string, int> GetPascalDict();
        Dictionary<string, int> GetCamelDict();
        Dictionary<string, int> GetLowerDict();
        Dictionary<string, int> GetUpperDict();
        string GetVirtualString();
        string GetOverrideString();
    }
}