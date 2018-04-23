using Emby.Kodi.SyncQueue.Entities;
using System.Collections.Generic;
using MediaBrowser.Model.Services;

namespace Emby.Kodi.SyncQueue.API
{
    //OLD METHOD LEFT IN FOR BACKWARDS COMPATIBILITY
    [Route("/Emby.Kodi.SyncQueue/{UserID}/{LastUpdateDT}/GetItems", "GET", Summary = "Gets Items for {USER} from {UTC DATETIME} formatted as yyyy-MM-ddThh:mm:ssZ")]
    public class GetLibraryItems : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
        public string filter { get; set; }
    }

    [Route("/Emby.Kodi.SyncQueue/{UserID}/GetItems", "GET", Summary = "Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT")]
    public class GetLibraryItemsQuery : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
        public string filter { get; set; }

    }

    [Route("/Emby.Kodi.SyncQueue/GetServerDateTime", "GET", Summary = "Gets The Server Time in UTC format as yyyy-MM-ddTHH:mm:ssZ")]
    public class GetServerTime : IReturn<ServerTimeInfo>
    {

    }

    [Route("/Emby.Kodi.SyncQueue/GetPluginSettings", "GET", Summary = "Get SyncQueue Plugin Settings")]
    public class GetPluginSettings : IReturn<PluginSettings>
    {

    }


    [Route("/Kodi/{Type}/{Id}/file.strm", "GET", Summary = "Create a dynamic strm")]
    [Route("/Kodi/{Type}/{ParentId}/{Id}/file.strm", "GET", Summary = "Create a dynamic strm")]
    [Route("/Kodi/{Type}/{ParentId}/{Season}/{Id}/file.strm", "GET", Summary = "Create a dynamic strm")]
    public class GetStrmFile
    {
        [ApiMember(Name = "Type", Description = "Media type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Type { get; set; }

        [ApiMember(Name = "Id", Description = "Item id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "KodiId", Description = "Kodi item id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string KodiId { get; set; }

        [ApiMember(Name = "Name", Description = "Strm name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "Handler", Description = "Optional handler", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Handler { get; set; }

        [ApiMember(Name = "ParentId", Description = "Parent id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "Season", Description = "Season number", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Season { get; set; }
    }

    [Route("/Kodi/{Id}/MyVideos107.db", "GET", Summary = "Get Kodi video database")]
    public class GetVideoDatabase
    {
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
