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
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;

namespace XmlMetadata
{
    public class EpisodeXmlProvider : IMetadataFileSaver
    {
        private readonly IItemRepository _itemRepository;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public EpisodeXmlProvider(IItemRepository itemRepository, IServerConfigurationManager config, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _itemRepository = itemRepository;
            _config = config;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
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

            return item is Episode && updateType >= ItemUpdateType.MetadataDownload;
        }

        public string Name
        {
            get
            {
                return Plugin.MetadataName;
            }
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;

            var builder = new StringBuilder();

            builder.Append("<Item>");

            if (!string.IsNullOrEmpty(item.Name))
            {
                builder.Append("<EpisodeName>" + SecurityElement.Escape(episode.Name) + "</EpisodeName>");
            }

            if (episode.IndexNumber.HasValue)
            {
                builder.Append("<EpisodeNumber>" + SecurityElement.Escape(episode.IndexNumber.Value.ToString(_usCulture)) + "</EpisodeNumber>");
            }

            if (episode.IndexNumberEnd.HasValue)
            {
                builder.Append("<EpisodeNumberEnd>" + SecurityElement.Escape(episode.IndexNumberEnd.Value.ToString(_usCulture)) + "</EpisodeNumberEnd>");
            }

            if (episode.AirsAfterSeasonNumber.HasValue)
            {
                builder.Append("<airsafter_season>" + SecurityElement.Escape(episode.AirsAfterSeasonNumber.Value.ToString(_usCulture)) + "</airsafter_season>");
            }
            if (episode.AirsBeforeEpisodeNumber.HasValue)
            {
                builder.Append("<airsbefore_episode>" + SecurityElement.Escape(episode.AirsBeforeEpisodeNumber.Value.ToString(_usCulture)) + "</airsbefore_episode>");
            }
            if (episode.AirsBeforeSeasonNumber.HasValue)
            {
                builder.Append("<airsbefore_season>" + SecurityElement.Escape(episode.AirsBeforeSeasonNumber.Value.ToString(_usCulture)) + "</airsbefore_season>");
            }

            if (episode.ParentIndexNumber.HasValue)
            {
                builder.Append("<SeasonNumber>" + SecurityElement.Escape(episode.ParentIndexNumber.Value.ToString(_usCulture)) + "</SeasonNumber>");
            }

            if (episode.AbsoluteEpisodeNumber.HasValue)
            {
                builder.Append("<absolute_number>" + SecurityElement.Escape(episode.AbsoluteEpisodeNumber.Value.ToString(_usCulture)) + "</absolute_number>");
            }

            if (episode.DvdEpisodeNumber.HasValue)
            {
                builder.Append("<DVD_episodenumber>" + SecurityElement.Escape(episode.DvdEpisodeNumber.Value.ToString(_usCulture)) + "</DVD_episodenumber>");
            }

            if (episode.DvdSeasonNumber.HasValue)
            {
                builder.Append("<DVD_season>" + SecurityElement.Escape(episode.DvdSeasonNumber.Value.ToString(_usCulture)) + "</DVD_season>");
            }

            if (episode.PremiereDate.HasValue)
            {
                builder.Append("<FirstAired>" + SecurityElement.Escape(episode.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd")) + "</FirstAired>");
            }

            XmlSaverHelpers.AddCommonNodes(episode, _libraryManager, builder);
            XmlSaverHelpers.AddMediaInfo(episode, builder, _itemRepository);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "FirstAired",
                    "SeasonNumber",
                    "EpisodeNumber",
                    "EpisodeName",
                    "EpisodeNumberEnd",
                    "airsafter_season",
                    "airsbefore_episode",
                    "airsbefore_season",
                    "DVD_episodenumber",
                    "DVD_season",
                    "absolute_number"

                }, _config, _fileSystem);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            var filename = Path.ChangeExtension(Path.GetFileName(item.Path), ".xml");

            return Path.Combine(Path.GetDirectoryName(item.Path), "metadata", filename);
        }
    }
}
