namespace Tests.Services {
    public class TestServiceImpl : TestService, ITestServiceImpl {
        internal const string SAMPLE_STRING = "TestServiceImpl";

        public string GetSampleString() {
            return SAMPLE_STRING;
        }


    }
}