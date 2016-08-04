using ServiceStack;
using Emby.Kodi.SyncQueue.Entities;
using System.Collections.Generic;

namespace Emby.Kodi.SyncQueue.API
{
    //OLD METHOD LEFT IN FOR BACKWARDS COMPATIBILITY
    [Route("/Emby.Kodi.SyncQueue/{UserID}/{LastUpdateDT}/GetItems", "GET", Summary = "Gets Items for {USER} from {UTC DATETIME} formatted as yyyy-MM-ddThh:mm:ssZ")]
    public class GetLibraryItems : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "movies", Description = "0 or 1 (default) of whether or not to include movies", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "tvshows", Description = "0 or 1 (default) of whether or not to include tvshows", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "music", Description = "0 or 1 (default) of whether or not to include music", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "musicvideos", Description = "0 or 1 (default) of whether or not to include musicvideos", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "boxsets", Description = "0 or 1 (default) of whether or not to include boxsets", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
        public int movies { get; set; }
        public int tvshows { get; set; }
        public int music { get; set; }
        public int musicvideos { get; set; }
        public int boxsets { get; set; }
        public GetLibraryItems()
        {
            movies = 1;
            tvshows = 1;
            music = 1;
            musicvideos = 1;
            boxsets = 1;
        }
    }

    [Route("/Emby.Kodi.SyncQueue/{UserID}/GetItems", "GET", Summary = "Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT")]
    public class GetLibraryItemsQuery : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "movies", Description = "0 or 1 (default) of whether or not to include movies", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "tvshows", Description = "0 or 1 (default) of whether or not to include tvshows", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "music", Description = "0 or 1 (default) of whether or not to include music", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "musicvideos", Description = "0 or 1 (default) of whether or not to include musicvideos", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "boxsets", Description = "0 or 1 (default) of whether or not to include boxsets", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
        public int movies { get; set; }
        public int tvshows { get; set; }
        public int music { get; set; }
        public int musicvideos { get; set; }
        public int boxsets { get; set; }
        public GetLibraryItemsQuery()
        {
            movies = 1;
            tvshows = 1;
            music = 1;
            musicvideos = 1;
            boxsets = 1;
        }
    }

    [Route("/Emby.Kodi.SyncQueue/{UserID}/GetItems", "POST", Summary = "Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT")]
    public class GetLibraryItemsQueryPost : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "movies", Description = "0 or 1 (default) of whether or not to include movies", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "tvshows", Description = "0 or 1 (default) of whether or not to include tvshows", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "music", Description = "0 or 1 (default) of whether or not to include music", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "musicvideos", Description = "0 or 1 (default) of whether or not to include musicvideos", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "boxsets", Description = "0 or 1 (default) of whether or not to include boxsets", IsRequired = false, DataType = "integer", ParameterType = "query", Verb = "POST")]
        [ApiMember(Name = "filterList", Description = "MediaLibraries to Ignore JSON String List { x, y, z }", IsRequired = false, ParameterType = "body", Verb = "POST")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
        public int movies { get; set; }
        public int tvshows { get; set; }
        public int music { get; set; }
        public int musicvideos { get; set; }
        public int boxsets { get; set; }
        public List<string> filterList { get; set; }
        public GetLibraryItemsQueryPost()
        {
            movies = 1;
            tvshows = 1;
            music = 1;
            musicvideos = 1;
            boxsets = 1;
            filterList = new List<string>();
        }
    }

    [Route("/Emby.Kodi.SyncQueue/GetServerDateTime", "GET", Summary = "Gets The Server Time in UTC format as yyyy-MM-ddTHH:mm:ssZ")]
    public class GetServerTime : IReturn<ServerTimeInfo>
    {
        
    }
}
