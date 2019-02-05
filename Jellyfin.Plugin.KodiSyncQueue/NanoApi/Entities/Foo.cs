using System;
using System.Collections.Generic;

namespace NanoApi.Entities
{
    internal class Foo<T>
    {
        public DbHeader _header { get; set; }
        public List<T> data { get; set; }
    }
}
