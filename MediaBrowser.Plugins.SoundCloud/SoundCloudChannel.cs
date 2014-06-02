﻿using System.Net.Mime;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Plugins.SoundCloud
{
    public class SoundCloudChannel : IChannel
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public SoundCloudChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "4";
            }
        }

        public bool IsEnabledFor(Controller.Entities.User user)
        {
            return true;
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (query.FolderId == null)
            {
                return await GetChannels(cancellationToken).ConfigureAwait(false);
            }
            
            var catSplit = query.FolderId.Split('_');
            query.FolderId = catSplit[1];
            return await GetTracks(query, cancellationToken).ConfigureAwait(false);
            

        }

        private async Task<ChannelItemResult> GetChannels(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>
            {
                new ChannelItemInfo
                {
                    Name = "Hot",
                    Id = "cat_hot",
                    Type = ChannelItemType.Folder
                },
                new ChannelItemInfo
                {
                    Name = "Latest",
                    Id = "cat_latest",
                    Type = ChannelItemType.Folder
                }
            };

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3),
            };
        }

        private async Task<ChannelItemResult> GetTracks(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var offset = query.StartIndex.GetValueOrDefault();
            var downloader = new SoundCloudListingDownloader(_logger, _jsonSerializer, _httpClient);
            var songs = await downloader.GetTrackList(query, cancellationToken)
                .ConfigureAwait(false);

            var tracks = songs.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Song,
                ImageUrl = i.artwork_url,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Audio,
                Name = i.title,
                Type = ChannelItemType.Media,
                Id = i.id.ToString(),
                RunTimeTicks = TimeSpan.FromMilliseconds(i.duration).Ticks,

                MediaSources = new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                        Path = i.stream_url + "?client_id=78fd88dde7ebf8fdcad08106f6d56ab6"
                    }
                }
            });

            var channelItemInfos = tracks as IList<ChannelItemInfo> ?? tracks.ToList();
            
            return new ChannelItemResult
            {
                Items = channelItemInfos.ToList(),
                CacheLength = TimeSpan.FromDays(3),
                TotalRecordCount = channelItemInfos.Count() + offset + 1

            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Primary,
                ImageType.Backdrop
            };
        }

        public string Name
        {
            get { return "SoundCloud"; }
        }



        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Song
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Audio
                  },
                  MaxPageSize = 200
            };
        }

        public string HomePageUrl
        {
            get { return "http://www.soundcloud.com/"; }
        }
    }
}
