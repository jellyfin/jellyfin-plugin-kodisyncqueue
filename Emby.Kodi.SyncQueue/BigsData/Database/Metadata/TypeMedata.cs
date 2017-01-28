using System;
using System.Collections.Generic;

namespace BigsData.Database.Metadata
{
    public class TypeMedata
    {
        public IEnumerable<PropertyMetadata> Properites { get; set; }
    }

    public class PropertyMetadata
    {
        public Type PropertyType { get; set; }
    }
}
