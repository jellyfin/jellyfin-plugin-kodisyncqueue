using ServiceStack;
using Emby.Kodi.SyncQueue.Entities;

namespace Emby.Kodi.SyncQueue.API
{
    //OLD METHOD LEFT IN FOR BACKWARDS COMPATIBILITY
    [Route("/Emby.Kodi.SyncQueue/{UserID}/{LastUpdateDT}/GetItems", "GET", Summary = "Gets Items for {USER} from {UTC DATETIME} formatted as yyyy-MM-ddThh:mm:ssZ")]
    public class GetLibraryItems : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
    }

    [Route("/Emby.Kodi.SyncQueue/{UserID}/GetItems", "GET", Summary = "Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT")]
    public class GetLibraryItemsQuery : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
    }

    [Route("/Emby.Kodi.SyncQueue/GetServerDateTime", "GET", Summary = "Gets The Server Time in UTC format as yyyy-MM-ddTHH:mm:ssZ")]
    public class GetServerTime : IReturn<ServerTimeInfo>
    {
        
    }
}
