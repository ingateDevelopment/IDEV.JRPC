using System;

namespace JRPC.Core.Security {

    /// <summary>
    /// Интерфейс для авторизации
    /// </summary>
    public interface IAbstractCredentials {

        string GetHeaderValue();

    }

}