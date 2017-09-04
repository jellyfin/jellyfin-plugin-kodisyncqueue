using NanoApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Emby.Kodi.SyncQueue.Data;

namespace NanoApi
{
    internal class File
    {
        private DateTime? lastTS;
        private string strDataCache;
        private static Dictionary<string, File> pool = new Dictionary<string, File>();
        private Impersonate impersonate;

        private string path { get; set; }
        private string filename { get; set; }
        private Encoding encoding { get; set; }
        private string impersonateLogin { get; set; }
        private string impersonatePassword { get; set; }

        private File(string path, string filename, Encoding encoding = null, string impersonateLogin = null, string impersonatePassword = null)
        {
            this.path = path;
            this.filename = filename;
            this.encoding = encoding;
            this.impersonateLogin = impersonateLogin;
            this.impersonatePassword = impersonatePassword;
        }

        public static File GetInstance(string path, string filename, Encoding encoding = null, string impersonateLogin = null, string impersonatePassword = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            string text = string.Concat(new string[]
            {
                path, "_", filename, "_", impersonateLogin, "_", impersonatePassword
            });
            File file;
            if (File.pool.Keys.Contains(text))
                file = File.pool[text];
            else
            {
                file = new NanoApi.File(path, filename, encoding, impersonateLogin, impersonatePassword);
                File.pool.Add(text, file);
            }
            return file;
        }

        public bool Save<T>(List<T> data)
        {
            Foo<T> foo = this.Read<T>();
            if (foo == null)
                foo = FooHelper.Create<T>();

            foo.data = data;
            return this.Save<T>(foo);
        }

        public bool Save<T>(Foo<T> foo)
        {
            foo._header.updateDate = new DateTime?(DateTime.UtcNow);
            string contents = DbRepo.json.SerializeToString(foo);
            DbRepo.fileSystem.CreateDirectory(this.path);
            string path = Path.Combine(this.path, this.filename);
            if (this.encoding == null)
                DbRepo.fileSystem.WriteAllText(path, contents);
            else
                DbRepo.fileSystem.WriteAllText(path, contents, this.encoding);
            this.strDataCache = contents;
            return true;
        }

        public bool DeleteFile()
        {
            string path = Path.Combine(this.path, this.filename);
            if (DbRepo.fileSystem.FileExists(path))
                DbRepo.fileSystem.DeleteFile(path);
            return true;
        }

        public Foo<T> Read<T>()
        {
            string path = Path.Combine(this.path, this.filename);
            if (!DbRepo.fileSystem.FileExists(path))
                return null;

            DateTime lastWriteTime = DbRepo.fileSystem.GetLastWriteTimeUtc(path);
            if (!this.lastTS.HasValue || this.lastTS.Value.Ticks != lastWriteTime.Ticks)
            {
                if (this.encoding == null)
                    this.strDataCache = DbRepo.fileSystem.ReadAllText(path);
                else
                    this.strDataCache = DbRepo.fileSystem.ReadAllText(path, this.encoding);
                this.lastTS = new DateTime?(lastWriteTime);
            }
            Foo<T> foo = DbRepo.json.DeserializeFromString<Foo<T>>(this.strDataCache);
            if (foo._header == null)
                foo._header = FooHelper.CreateHeader();
            if (foo.data == null)
                foo.data = new List<T>();
            return foo;
        }
    }
}
