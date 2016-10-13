using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace VirtualMuseumAPI.Helpers
{
    public class VirtualMuseumDataResult : IHttpActionResult
    {
        private readonly MemoryStream _stream;
        private readonly string _mediaType;

        public VirtualMuseumDataResult(MemoryStream stream, string mediaType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (mediaType == null)
            {
                throw new ArgumentNullException("mediaType");
            } 
            _stream = stream;
            _mediaType = mediaType;

        }

        public MemoryStream getStream()
        {
            return _stream;
        }

        public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(System.Threading.CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(_stream)
                };

                response.Content.Headers.ContentType = new MediaTypeHeaderValue(_mediaType);

                return response;

            }, cancellationToken);
        }
    }
}