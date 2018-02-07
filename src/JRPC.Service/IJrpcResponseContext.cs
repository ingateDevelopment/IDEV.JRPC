using System.Collections.Generic;
using System.IO;

namespace JRPC.Service
{
    public interface IJrpcResponseContext
    {
        /// <summary>
        /// The status-code as defined in RFC 7230. The default value is 200.
        /// </summary>
        int StatusCode { get; set; }
        /// <summary>
        /// The reason-phrase as defined in RFC 7230. Note this field is no longer supported by HTTP/2.
        /// </summary>
        string ReasonPhrase { get; set; }
        /// <summary>
        /// Headers included in the request, aggregated by header name. The values are not split
        /// or merged across header lines. E.g. The following headers:
        /// HeaderA: value1, value2
        /// HeaderA: value3
        /// Result in Headers["HeaderA"] = { "value1, value2", "value3" }
        /// </summary>
        IDictionary<string, string[]> Headers { get; set; }
        /// <summary>
        /// The <see cref="T:System.IO.Stream" /> for writing the response body.
        /// </summary>
        Stream Body { get; set; }
        /// <summary>Gets or sets the Content-Type header.</summary>
        /// <returns>The Content-Type header.</returns>
        string ContentType { get; set; }
    }
}