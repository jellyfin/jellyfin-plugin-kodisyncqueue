using System.IO;

namespace BigsData.Database
{
    public struct ItemReference
    {
        internal ItemReference(string root, string database, string collection, string id)
        {
            Root = root;
            Database = database;
            Collection = collection;
            Id = id;
        }

        public string Id { get; private set; }
        public string Collection { get; private set; }
        public string Database { get; private set; }
        public string FullPath
        {
            get { return Path.Combine(CollectionPath, Id); }
        }
        public string CollectionPath
        {
            get
            {
                return Path.Combine(
                  Constants.DatabasesFolder,
                  Database,
                  Constants.CollectionsFolder,
                  Collection);
            }
        }

        internal static ItemReference Emtpy
        {
            get { return new ItemReference(string.Empty, string.Empty, string.Empty, string.Empty); }
        }

        public bool IsEmpty
        {
            get
            {
                return
                  string.IsNullOrEmpty(Database)
                  ||
                  string.IsNullOrEmpty(Collection)
                  ||
                  string.IsNullOrEmpty(Id);
            }
        }

        internal string Root { get; private set; }
        internal string RootFullPath
        {
            get { return Path.Combine(Root, FullPath); }
        }
        internal string RootCollectionPath
        {
            get { return Path.Combine(Root, CollectionPath); }
        }
    }
}
