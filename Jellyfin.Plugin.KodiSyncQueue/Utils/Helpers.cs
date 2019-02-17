using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaType = Jellyfin.Plugin.KodiSyncQueue.Entities.MediaType;

namespace Jellyfin.Plugin.KodiSyncQueue.Utils
{
    public static class Helpers
    {
        public static bool FilterAndGetMediaType(BaseItem item, out MediaType type)
        {
            type = MediaType.None;

            if (!Plugin.Instance.Configuration.IsEnabled ||
                item.LocationType == LocationType.Virtual ||
                item.SourceType != SourceType.Library)
            {
                return false;
            }


            var typeName = item.GetClientTypeName();
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            switch (typeName)
            {
                case "Movie":
                    if (!Plugin.Instance.Configuration.tkMovies)
                    {
                        return false;
                    }
                    type = MediaType.Movies;
                    break;
                case "BoxSet":
                    if (!Plugin.Instance.Configuration.tkBoxSets)
                    {
                        return false;
                    }
                    type = MediaType.BoxSets;
                    break;
                case "Series":
                case "Season":
                case "Episode":
                    if (!Plugin.Instance.Configuration.tkTVShows)
                    {
                        return false;
                    }
                    type = MediaType.TvShows;
                    break;
                case "Audio":
                case "MusicArtist":
                case "MusicAlbum":
                    if (!Plugin.Instance.Configuration.tkMusic)
                    {
                        return false;
                    }
                    type = MediaType.Music;
                    break;
                case "MusicVideo":
                    if (!Plugin.Instance.Configuration.tkMusicVideos)
                    {
                        return false;
                    }
                    type = MediaType.MusicVideos;
                    break;
                default:
                    return false;
            }                                   

            return true;
        }
    }
}