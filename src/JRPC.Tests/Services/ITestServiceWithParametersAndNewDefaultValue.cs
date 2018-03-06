namespace Tests.Services {
    public interface ITestServiceWithParametersAndNewDefaultValue {
        string MethodWithDefaulParameter(string par, string defaultParameter = TestServiceWithParameters.NEW_DEFAULT_PARAMETER_VALUE);
        string MethodWithParameters(string par1, string nonDefaultParameter);
    }
}