using System;

namespace NanoApi.JsonFile
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public bool auto = true;
    }
}
