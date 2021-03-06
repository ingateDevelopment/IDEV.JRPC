﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JRPC.Core.Security;

namespace JRPC.Client {

    public interface IJRpcClient {

        Task<TResult> Call<TResult>(JrpcClientCallParams clientCallParams);
        T GetProxy<T>(string taskName) where T : class;

    }

}