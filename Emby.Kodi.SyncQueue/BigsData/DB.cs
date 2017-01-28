using BigsData.Database;

namespace BigsData
{
    public static class DB
    {
        public static BigsDatabase Open(string baseFolder, string defaultDatabase = Constants.DefaultDatabaseName, string defaultCollection = Constants.DefaultCollectionName, bool failSilentlyOnReads = true, bool trackReferences = false)
        {
            return new BigsDatabase(baseFolder, defaultDatabase, defaultCollection, failSilentlyOnReads, trackReferences);
        }
    }
}