using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MovieOrganizer.Api;
using MovieOrganizer.Service;
using System.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using MovieOrganizer.Html;

namespace MovieOrganizer
{
    /// <summary>
    /// All communication between the server and the plugins server instance should occur in this class.
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpResultFactory _resultFactory;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IServerManager _serverManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileOrganizationService _fileOrganizationService;

        private MovieOrganizerApi _api;
        private MovieOrganizerService _service;
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
        public ServerEntryPoint(
            IServerConfigurationManager configurationManager, 
            ILibraryManager libraryManager,
            ILibraryMonitor libraryMonitor, 
            ILogManager logger, 
            IServerApplicationHost appHost,
            IHttpResultFactory resultFactory, 
            IFileSystem fileSystem,
            ILocalizationManager localizationManager,
            IServerManager serverManager,
            IProviderManager providerManager,
            IFileOrganizationService fileOrganizationService)
        {
            Instance = this;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
            _logger = logger.GetLogger("MovieOrganizer");
            _appHost = appHost;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _resultFactory = resultFactory;
            _localizationManager = localizationManager;
            _serverManager = serverManager;
            _providerManager = providerManager;
            _fileOrganizationService = fileOrganizationService;

            HtmlHelper.InstallFiles(Plugin.Instance.AppPaths, Plugin.Instance.PluginConfiguration);

            _service = new MovieOrganizerService(
                _configurationManager, 
                logger, 
                _fileSystem, 
                _appHost,
                _libraryMonitor,
                _libraryManager,
                _localizationManager,
                _serverManager,
                _providerManager,
                _fileOrganizationService);

            _api = new MovieOrganizerApi(logger, _service, _libraryManager);

            _staticFileServer = new StaticFileServer(logger, _resultFactory);
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