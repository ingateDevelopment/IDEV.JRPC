namespace Tests.Services {
    public class TestServiceImplWithOverriding : TestService, ITestServiceImpl {
        internal const string IMPL_STRING = "TestServiceImplWithOverriding";
        internal const string SAMPLE_STRING = "TestServiceImpl";

        public string GetSampleString() {
            return SAMPLE_STRING;
        }

        public override string GetVirtualString() {
            return IMPL_STRING;
        }

        public new string GetOverrideString() {
            return IMPL_STRING;
        }

    }
}