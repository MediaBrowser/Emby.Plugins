﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class EpisodeLocalLocalImageProvider : ILocalImageFileProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;

        public EpisodeLocalLocalImageProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Local Images"; }
        }

        public int Order
        {
            get { return 0; }
        }

        public bool Supports(IHasMetadata item)
        {
            return item is Episode && item.SupportsLocalMetadata;
        }

        public List<LocalImageInfo> GetImages(IHasMetadata item, IDirectoryService directoryService)
        {
            var parentPath = _fileSystem.GetDirectoryName(item.Path);

            var parentPathFiles = directoryService.GetFileSystemEntries(parentPath)
                .ToList();

            var nameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(item.Path);

            var metadataPath = Path.Combine(parentPath, "metadata");

            if (parentPathFiles.Any(i => string.Equals(i.FullName, metadataPath, StringComparison.OrdinalIgnoreCase)))
            {
                var filesInMetadataFolder = _fileSystem.GetFiles(metadataPath, BaseItem.SupportedImageExtensions, false, false);
                return GetFilesFromParentFolder(nameWithoutExtension, filesInMetadataFolder);
            }

            return new List<LocalImageInfo>();
        }

        private List<LocalImageInfo> GetFilesFromParentFolder(string filenameWithoutExtension, IEnumerable<FileSystemMetadata> parentPathFiles)
        {
            var thumbName = filenameWithoutExtension + "-thumb";

            return parentPathFiles
              .Where(i =>
              {
                  if (i.IsDirectory)
                  {
                      return false;
                  }
                  
                  if (BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                  {
                      var currentNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(i);

                      if (string.Equals(filenameWithoutExtension, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }

                      if (string.Equals(thumbName, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }
                  }

                  return false;
              })
              .Select(i => new LocalImageInfo
              {
                  FileInfo = i,
                  Type = ImageType.Primary
              })
              .ToList();
        }
    }
}
