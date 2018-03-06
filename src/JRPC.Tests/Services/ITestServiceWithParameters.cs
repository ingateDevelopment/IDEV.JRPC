namespace Tests.Services {
    public interface ITestServiceWithParameters {
        string MethodWithDefaulParameter(string par, string defaultParameter = TestServiceWithParameters.DEFAULT_PARAMETER_VALUE);
        string MethodWithParameters(string par1, string nonDefaultParameter);
    }
}