using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace JRPC.Service.Host.Kestrel {
    public class KestrelJrpcRequestContext : IJrpcRequestContext {
        private readonly IHttpRequestFeature _requestFeature;
        private readonly IHttpConnectionFeature _httpConnectionFeature;

        public KestrelJrpcRequestContext(IHttpRequestFeature requestFeature, IHttpConnectionFeature httpConnectionFeature) {
            _requestFeature = requestFeature;
            _httpConnectionFeature = httpConnectionFeature;
        }


        public string Protocol{
            get => _requestFeature.Protocol;
            set => _requestFeature.Protocol = value;
        }

        public string Scheme{
            get => _requestFeature.Scheme;
            set => _requestFeature.Scheme = value;
        }

        public string Method{
            get => _requestFeature.Method;
            set => _requestFeature.Method = value;
        }

        public string PathBase{
            get => _requestFeature.PathBase;
            set => _requestFeature.PathBase = value;
        }

        public string Path{
            get => _requestFeature.Path;
            set => _requestFeature.Path = value;
        }

        public string QueryString{
            get => _requestFeature.QueryString;
            set => _requestFeature.QueryString = value;
        }

        public string RawTarget{
            get => _requestFeature.RawTarget;
            set => _requestFeature.RawTarget = value;
        }

        public IDictionary<string, string[]> Headers{
            get { return _requestFeature.Headers?.ToDictionary(s => s.Key, s => s.Value.ToArray()); }
            set {
                _requestFeature.Headers =
                    (IHeaderDictionary) value?.ToDictionary(s => s.Key, s => (StringValues) s.Value);
            }
        }

        public Stream Body{
            get => _requestFeature.Body;
            set => _requestFeature.Body = value;
        }

        public string RemoteIpAddress{
            get => _httpConnectionFeature.RemoteIpAddress.ToString();
        }
    }
}