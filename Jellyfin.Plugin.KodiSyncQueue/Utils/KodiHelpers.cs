using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaType = Jellyfin.Plugin.KodiSyncQueue.Entities.MediaType;

namespace Jellyfin.Plugin.KodiSyncQueue.Utils
{
    public static class KodiHelpers
    {
        public static bool FilterAndGetMediaType(BaseItem item, out MediaType type)
        {
            type = MediaType.None;

            if (!KodiSyncQueuePlugin.Instance.Configuration.IsEnabled ||
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
                    if (!KodiSyncQueuePlugin.Instance.Configuration.TkMovies)
                    {
                        return false;
                    }

                    type = MediaType.Movies;
                    break;
                case "BoxSet":
                    if (!KodiSyncQueuePlugin.Instance.Configuration.TkBoxSets)
                    {
                        return false;
                    }

                    type = MediaType.BoxSets;
                    break;
                case "Series":
                case "Season":
                case "Episode":
                    if (!KodiSyncQueuePlugin.Instance.Configuration.TkTvShows)
                    {
                        return false;
                    }

                    type = MediaType.TvShows;
                    break;
                case "Audio":
                case "MusicArtist":
                case "MusicAlbum":
                    if (!KodiSyncQueuePlugin.Instance.Configuration.TkMusic)
                    {
                        return false;
                    }

                    type = MediaType.Music;
                    break;
                case "MusicVideo":
                    if (!KodiSyncQueuePlugin.Instance.Configuration.TkMusicVideos)
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
