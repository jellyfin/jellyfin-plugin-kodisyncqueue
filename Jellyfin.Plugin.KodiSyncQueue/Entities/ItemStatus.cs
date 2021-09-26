namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public enum ItemStatus
    {
        /// <summary>
        /// Item was added.
        /// </summary>
        Added = 0,

        /// <summary>
        /// Item was updated.
        /// </summary>
        Updated = 1,

        /// <summary>
        /// Item was removed.
        /// </summary>
        Removed = 2
    }
}
