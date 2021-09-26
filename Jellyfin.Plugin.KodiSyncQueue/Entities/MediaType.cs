namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public enum MediaType
    {
        /// <summary>
        /// No media type.
        /// </summary>
        None = -1,

        /// <summary>
        /// Movie media type.
        /// </summary>
        Movies = 0,

        /// <summary>
        /// Tv show media type.
        /// </summary>
        TvShows = 1,

        /// <summary>
        /// Music media type.
        /// </summary>
        Music = 2,

        /// <summary>
        /// Music video media type.
        /// </summary>
        MusicVideos = 3,

        /// <summary>
        /// Box sets media type.
        /// </summary>
        BoxSets = 4
    }
}
