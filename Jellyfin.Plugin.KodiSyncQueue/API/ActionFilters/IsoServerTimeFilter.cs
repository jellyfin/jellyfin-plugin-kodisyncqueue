#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.KodiSyncQueue.API.ActionFilters
{
    public class IsoServerTimeFilter : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context?.HttpContext.Response.Headers.TryAdd("Server-Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        }
    }
}
