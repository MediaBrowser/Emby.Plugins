﻿using MediaBrowser.Model.Services;

namespace OneDrive.RestServices
{
    [Route("/OneDrive/SyncTarget/{Id}", "DELETE")]
    public class DeleteSyncTarget : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
