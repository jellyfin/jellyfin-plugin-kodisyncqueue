using System;

namespace BigsData.Database.Metadata
{
    public sealed class CollectionMetadata
    {
        public string CollectionName { get; set; }
        public Type ItemType { get; set; }
    }
}
