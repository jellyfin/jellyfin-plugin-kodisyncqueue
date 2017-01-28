namespace BigsData.Database
{
    public static class Constants
    {
        private const string _sysPrefix = "_";
        public const string DefaultDatabaseName = "db";
        public const string DefaultCollectionName = "col";
        public const string DatabasesFolder = "_dbs";
        public const string CollectionsFolder = _sysPrefix + "cols";
        public const string MetadataFolder = _sysPrefix + "_md";
        public const string CollectionMetadataFolder = MetadataFolder + "/cols";
        public const string TypeMetadataFolder = MetadataFolder + "/ts";
    }
}
