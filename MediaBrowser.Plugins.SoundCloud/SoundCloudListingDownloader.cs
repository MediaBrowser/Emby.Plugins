﻿using System.Collections.Generic;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud
{
    public class SoundCloudListingDownloader
    {
        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public SoundCloudListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<List<RootObject>> GetTrackList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            // need to get this working. Its not returning a limit or a start index value? ...
            var url = "";
            var offset = query.StartIndex.GetValueOrDefault();

            var limit = query.Limit.HasValue ? query.Limit.Value : 50;
            
            if (query.FolderId == "hot")
                url = "http://api.soundcloud.com/tracks.json?client_id=78fd88dde7ebf8fdcad08106f6d56ab6&filter=streamable&limit="+ limit +"&order=hotness&offset=" + offset;
            if (query.FolderId == "latest")
                url = "http://api.soundcloud.com/tracks.json?client_id=78fd88dde7ebf8fdcad08106f6d56ab6&filter=streamable&limit=" + limit + "&order=created_at&offset=" + offset;
            
            using (var json = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<List<RootObject>>(json);
            }
        }
       
    }
}
