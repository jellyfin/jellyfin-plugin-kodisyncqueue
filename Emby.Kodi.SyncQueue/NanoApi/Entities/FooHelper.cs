using System;
using System.Collections.Generic;

namespace NanoApi.Entities
{
    internal static class FooHelper
    {
        private const string version = "0.1.03";

        private const string descriptor = "Little Json format: http://www.jsonfile.com";

        public static DbHeader CreateHeader()
        {
            return new DbHeader
            {
                createDate = DateTimeOffset.UtcNow,
                version = "0.1.03",
                descriptor = "Little Json format: http://www.jsonfile.com",
                idMax = 0
            };
        }

        public static Foo<T> Create<T>()
        {
            return new Foo<T>
            {
                _header = FooHelper.CreateHeader(),
                data = new List<T>()
            };
        }
    }
}
