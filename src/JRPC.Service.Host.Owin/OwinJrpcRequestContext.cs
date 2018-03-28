using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Owin;

namespace JRPC.Service.Host.Owin {
    public class OwinJrpcRequestContext : IJrpcRequestContext {
        private readonly IOwinRequest _request;

        public OwinJrpcRequestContext(IOwinRequest request) {
            _request = request;
        }

        public string Protocol{
            get => _request.Protocol;
            set => _request.Protocol = value;
        }

        public string Scheme{
            get => _request.Scheme;
            set => _request.Scheme = value;
        }

        public string Method{
            get => _request.Method;
            set => _request.Method = value;
        }

        public string PathBase{
            get => _request.PathBase.HasValue ? _request.PathBase.Value : null;
            set => _request.PathBase = new PathString(value);
        }

        public string Path{
            get => _request.Path.HasValue ? _request.Path.Value : null;
            set => _request.Path = new PathString(value);
        }

        public string QueryString{
            get => _request.QueryString.HasValue ? _request.QueryString.Value : null;
            set => _request.QueryString = new QueryString(value);
        }

        public string RawTarget{
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public IDictionary<string, string[]> Headers{
            get => _request.Headers;
            set => throw new NotSupportedException();
        }

        public Stream Body{
            get => _request.Body;
            set => _request.Body = value;
        }

        public string RemoteIpAddress{
            get => _request.RemoteIpAddress;
        }
    }
}