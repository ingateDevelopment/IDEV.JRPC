using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace JRPC.Service.Host.Kestrel {
    public class KestrelJrpcResponseContext : IJrpcResponseContext {
        private readonly IHttpResponseFeature _responseFeature;

        private const string CONTENT_TYPE_HEADER_NAME = "content-type";
        public KestrelJrpcResponseContext(IHttpResponseFeature responseFeature) {
            _responseFeature = responseFeature;
        }

        public int StatusCode{
            get => _responseFeature.StatusCode;
            set => _responseFeature.StatusCode = value;
        }

        public string ReasonPhrase{
            get => _responseFeature.ReasonPhrase;
            set => _responseFeature.ReasonPhrase = value;
        }

        public IDictionary<string, string[]> Headers{
            get { return _responseFeature.Headers?.ToDictionary(s => s.Key, s => s.Value.ToArray()); }
            set {
                _responseFeature.Headers =
                    (IHeaderDictionary) value?.ToDictionary(s => s.Key, s => (StringValues) s.Value);
            }
        }

        public Stream Body{
            get => _responseFeature.Body;
            set => _responseFeature.Body = value;
        }

        public string ContentType{
            get => _responseFeature.Headers.ContainsKey(CONTENT_TYPE_HEADER_NAME)
                ? _responseFeature.Headers[CONTENT_TYPE_HEADER_NAME].FirstOrDefault()
                : null;
            set => _responseFeature.Headers[CONTENT_TYPE_HEADER_NAME] = new[] { value };
        }
    }
}