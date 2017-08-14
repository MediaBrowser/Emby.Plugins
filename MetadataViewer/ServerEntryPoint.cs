using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MetadataViewer;
using MetadataViewer.Api;
using MetadataViewer.Service;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using MetadataViewer.Html;

namespace Trakt
{
    /// <summary>
    /// All communication between the server and the plugins server instance should occur in this class.
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IHttpResultFactory _resultFactory;
        private MetadataViewerApi _api;
        private MetadataViewerService _service;
        private StaticFileServer _staticFileServer;

        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonSerializer"></param>
        /// <param name="sessionManager"> </param>
        /// <param name="userDataManager"></param>
        /// <param name="libraryManager"> </param>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        /// <param name="appHost"></param>
        /// <param name="fileSystem"></param>
        public ServerEntryPoint(IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogManager logger, IServerApplicationHost appHost, IHttpResultFactory resultFactory, IFileSystem fileSystem)
        {
            _logger = logger.GetLogger("MetadataViewer");
            _logger.Info("[MetadataViewer] ServerEntryPoint");

            Instance = this;
            _libraryManager = libraryManager;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _resultFactory = resultFactory;

            HtmlHelper.InstallFiles(Plugin.Instance.AppPaths, Plugin.Instance.PluginConfiguration);

            _service = new MetadataViewerService(_configurationManager, logger, _fileSystem, _appHost);
            _api = new MetadataViewerApi(logger, _service, _libraryManager);
            _staticFileServer = new StaticFileServer(logger, _resultFactory);

            _logger.Info("[MetadataViewer] ServerEntryPoint Exit");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }
    }
}