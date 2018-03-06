namespace Tests.Services {
    public interface ITestServiceImplWithOverridingByParams {
        string Method(string par);
        string Method(string par1, string par2);
    }
}