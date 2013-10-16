﻿using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Plugins.XbmcMetadata.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public Guid? UserId { get; set; }

        public List<Replacement> PathSubstitutions { get; set; }

        public PluginConfiguration()
        {
            PathSubstitutions = new List<Replacement>();
        }
    }

    public class Replacement
    {
        public string From { get; set; }
        public string To { get; set; }
    }
}
