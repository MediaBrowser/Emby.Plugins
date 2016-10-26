﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace XmlMetadata
{
    public class SeriesXmlProvider : IMetadataFileSaver
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public SeriesXmlProvider(IServerConfigurationManager config, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _config = config;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get
            {
                return Plugin.MetadataName;
            }
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Series && updateType >= ItemUpdateType.MetadataDownload;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var series = (Series)item;

            var builder = new StringBuilder();

            builder.Append("<Series>");

            var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(tvdb) + "</id>");
            }

            if (series.Status.HasValue)
            {
                builder.Append("<Status>" + SecurityElement.Escape(series.Status.Value.ToString()) + "</Status>");
            }

            if (series.Studios.Count > 0)
            {
                builder.Append("<Network>" + SecurityElement.Escape(series.Studios[0]) + "</Network>");
            }

            if (!string.IsNullOrEmpty(series.AirTime))
            {
                builder.Append("<Airs_Time>" + SecurityElement.Escape(series.AirTime) + "</Airs_Time>");
            }

            if (series.AirDays != null)
            {
                if (series.AirDays.Count == 7)
                {
                    builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape("Daily") + "</Airs_DayOfWeek>");
                }
                else if (series.AirDays.Count > 0)
                {
                    builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape(series.AirDays[0].ToString()) + "</Airs_DayOfWeek>");
                }
            }

            if (series.PremiereDate.HasValue)
            {
                builder.Append("<FirstAired>" + SecurityElement.Escape(series.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd")) + "</FirstAired>");
            }

            if (series.AnimeSeriesIndex.HasValue)
            {
                builder.Append("<AnimeSeriesIndex>" + SecurityElement.Escape(series.AnimeSeriesIndex.Value.ToString(UsCulture)) + "</AnimeSeriesIndex>");
            }

            XmlSaverHelpers.AddCommonNodes(series, _libraryManager, builder);

            builder.Append("</Series>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "id", 
                    "Status",
                    "Network",
                    "Airs_Time",
                    "Airs_DayOfWeek",
                    "FirstAired",

                    // Don't preserve old series node
                    "Series",

                    "SeriesName",

                    // Deprecated. No longer saving in this field.
                    "AnimeSeriesIndex"
                }, _config, _fileSystem);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "series.xml");
        }
    }
}
