using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JRPC.Service.Regestry;
using JRPC.Service;

namespace JRPC.Regestry.Ninject {
    public class NinjectModulesRegestry : IModulesRegestry {

        private readonly IKernel _kernel;

        public NinjectModulesRegestry(IKernel kernel) {
            _kernel = kernel;
        }

        public Dictionary<string, JRpcModule> GetAllServices() {
            return _kernel.GetAll<JRpcModule>().ToDictionary(t => t.ModuleName, t => t);
        }
    }
}
