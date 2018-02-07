using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Owin;

namespace JRPC.Service.Host.Owin {
    public class OwinJrpcResponseContext : IJrpcResponseContext {
        private readonly IOwinResponse _response;

        public OwinJrpcResponseContext(IOwinResponse response) {
            _response = response;
        }

        public int StatusCode{
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }

        public string ReasonPhrase{
            get => _response.ReasonPhrase;
            set => _response.ReasonPhrase = value;
        }

        public IDictionary<string, string[]> Headers{
            get => _response.Headers;
            set => throw new NotSupportedException();
        }

        public Stream Body{
            get => _response.Body;
            set => _response.Body = value;
        }

        public string ContentType{
            get => _response.ContentType;
            set => _response.ContentType = value;
        }
    }
}