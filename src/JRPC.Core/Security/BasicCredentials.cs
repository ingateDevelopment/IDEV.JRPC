namespace JRPC.Core.Security {

    /// <summary>
    /// Класс базовой авторизации
    /// </summary>
    public class BasicCredentials : IAbstractCredentials {

        private string _basic;

        public BasicCredentials(string login, string password) {
            _basic = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(login + ":" + password));
        }

        public string GetHeaderValue() {
            return _basic;
        }

    }

}