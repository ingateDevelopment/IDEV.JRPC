using Ninject;
using System.Collections.Generic;
using System.Linq;
using JRPC.Service.Registry;
using JRPC.Service;

namespace JRPC.Registry.Ninject {
    public class NinjectModulesRegistry : IModulesRegistry {

        private readonly IKernel _kernel;

        public NinjectModulesRegistry(IKernel kernel) {
            _kernel = kernel;
        }

        public Dictionary<string, JRpcModule> GetAllServices() {
            return _kernel.GetAll<JRpcModule>().ToDictionary(t => t.ModuleName, t => t);
        }
    }
}
