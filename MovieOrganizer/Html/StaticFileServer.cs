using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace MovieOrganizer.Html
{
    [Route("/web/components/fileorganizer/fileorganizer.js", "GET")]
    [Route("/web/components/fileorganizer/fileorganizer.template.html", "GET")]
    public class GetStaticResource
    {
    }

    public class StaticFileServer : IService, IRequiresRequest
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        private readonly IHttpResultFactory _resultFactory;

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        public StaticFileServer(ILogManager logManager, IHttpResultFactory resultFactory)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _resultFactory = resultFactory;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetStaticResource request)
        {
            var path = Request.PathInfo;

            var contentType = MimeTypes.GetMimeType(path);

            MemoryStream resultStream = null;

            if (path.Contains("/components/fileorganizer/fileorganizer.js"))
            {
                resultStream = HtmlHelper.OrganizerScript;
            }
            else if (path.Contains("/components/fileorganizer/fileorganizer.template.html"))
            {
                resultStream = HtmlHelper.OrganizerTemplate;
            }

            if (resultStream != null)
            {
                resultStream = new MemoryStream(resultStream.GetBuffer());
            }

            return _resultFactory.GetResult(resultStream, contentType);
        }
    }
}