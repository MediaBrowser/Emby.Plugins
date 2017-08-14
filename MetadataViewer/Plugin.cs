using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MetadataViewer.Configuration;

namespace MetadataViewer
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        IApplicationPaths _appPaths;

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            //_logger.Info("[MetadataViewer] ServerEntryPoint");

            Instance = this;
            _appPaths = appPaths;
        }

        public override string Name
        {
            get { return StaticName; }
        }

        public static string StaticName
        {
            get { return "Metadata Viewer"; }
        }

        public override string Description
        {
            get
            {
                return "Displays a table of raw metadata from all remote providers";
            }
        }

        public override void OnUninstalling()
        {
            HtmlHelper.UninstallFiles(_appPaths, PluginConfiguration);
            base.OnUninstalling();
        }

        public static Plugin Instance { get; private set; }

        public IApplicationPaths AppPaths
        {
            get { return _appPaths; }
        }

        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }
    }
}
