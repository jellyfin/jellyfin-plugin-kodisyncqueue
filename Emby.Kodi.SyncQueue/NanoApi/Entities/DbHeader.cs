using System;

namespace NanoApi.Entities
{
    internal class DbHeader
    {
        public string descriptor { get; set; }
        public string version { get; set; }
        public string title { get; set; }
        public DateTimeOffset? createDate { get; set; }
        public DateTimeOffset? updateDate { get; set; }
        public int idMax { get; set; }
    }
}
