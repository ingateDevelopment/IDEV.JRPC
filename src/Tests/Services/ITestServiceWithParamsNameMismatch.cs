namespace Tests.Services {
    public interface ITestServiceWithParamsNameMismatch {
        string Method(string par1);
        string MethodWihInterfecaIntersect(string par1);
    }

    public interface ITestServiceWithParamsNameMismatch2 {

        string AnotherMethod(string parameter);
    }

    public interface ITestServiceWithInterfaceMethodIntersectAndParameterNameMismatch {
        string MethodWihInterfecaIntersect(string parameter);
    }

    public interface ITestServiceWithInterfaceMethodIntersect {
        string MethodWihInterfecaIntersect(string par1);
    }
}