namespace JRPC.Core.Security {

    public class BasicCredentials : AbstractCredentials {

        private string _basic;

        public BasicCredentials(string login, string password) {
            _basic = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(login + ":" + password));
        }

        public override string GetHeaderValue() {
            return _basic;
        }

    }

}