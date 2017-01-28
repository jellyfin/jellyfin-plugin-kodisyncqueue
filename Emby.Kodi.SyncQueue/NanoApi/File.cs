using NanoApi.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

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

        private WindowsImpersonationContext GetImpersonate()
        {
            if (this.impersonateLogin == null)
                return null;

            if (impersonate == null)
                impersonate = new Impersonate(this.impersonateLogin, this.impersonatePassword);

            return impersonate.GetImpersonate();
        }

        public static File GetInstance(string path, string filename, Encoding encoding = null, string impersonateLogin = null, string impersonatePassword = null)
        {
            if (encoding == null)
                encoding = Encoding.Default;

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
            new JsonSerializerSettings();
            string contents = JsonConvert.SerializeObject(foo, Formatting.Indented);
            using (this.GetImpersonate())
            {
                Directory.CreateDirectory(this.path);
                string path = Path.Combine(this.path, this.filename);
                if (this.encoding == null)
                    System.IO.File.WriteAllText(path, contents);
                else
                    System.IO.File.WriteAllText(path, contents, this.encoding);
            }
            return true;
        }

        private static string JsonPrettify(string json)
        {
            string result;
            using (StringReader stringReader = new StringReader(json))
            {
                using (StringWriter stringWriter = new StringWriter())
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(stringReader))
                    {
                        JsonTextWriter expr_1A = new JsonTextWriter(stringWriter);
                        expr_1A.Formatting = Formatting.Indented;
                        expr_1A.Indentation = 1;
                        expr_1A.IndentChar = '\t';
                        using(JsonTextWriter jsonTextWriter = expr_1A)
                        {
                            jsonTextWriter.WriteToken(jsonTextReader);
                            result = stringWriter.ToString();
                        }
                    }
                }
            }
            return result;
        }

        public bool DeleteFile()
        {
            string path = Path.Combine(this.path, this.filename);
            using (this.GetImpersonate())
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            return true;
        }

        public Foo<T> Read<T>()
        {
            using (this.GetImpersonate())
            {
                string path = Path.Combine(this.path, this.filename);
                if (!System.IO.File.Exists(path))
                    return null;

                DateTime lastWriteTime = System.IO.File.GetLastWriteTime(path);
                if (!this.lastTS.HasValue || this.lastTS.Value.Ticks != lastWriteTime.Ticks)
                {
                    if (this.encoding == null)
                        this.strDataCache = System.IO.File.ReadAllText(path);
                    else
                        this.strDataCache = System.IO.File.ReadAllText(path, this.encoding);
                    this.lastTS = new DateTime?(lastWriteTime);
                }
            }
            Foo<T> foo = JsonConvert.DeserializeObject<Foo<T>>(this.strDataCache);
            if (foo._header == null)
                foo._header = FooHelper.CreateHeader();
            if (foo.data == null)
                foo.data = new List<T>();
            return foo;
        }
    }
}
