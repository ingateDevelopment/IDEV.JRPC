using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRPC.Service.Regestry {
    public interface IModulesRegestry {
        Dictionary<string, JRpcModule> GetAllServices();
    }
}
