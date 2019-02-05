using NanoApi.Entities;
using NanoApi.JsonFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NanoApi
{
    public class JsonFile<T> : IDisposable where T : class
    {
        private static Dictionary<string, JsonFile<T>> pool = new Dictionary<string, JsonFile<T>>();

        private string path { get; set; }
        private string filename { get; set; }
        private Encoding encoding { get; set; }
        private File file { get; set; }
        public string title { get; set; }
        public string description { get; set; }

        public static JsonFile<T> GetInstance(string path, string filename, Encoding encoding = null, string impersonateLogin = null, string impersonatePassword = null)
        {
            string text = string.Concat(new string[]
            {
                path, "_", filename, "_",impersonateLogin, "_",impersonatePassword
            });
            JsonFile<T> jsonFile;
            if (JsonFile<T>.pool.Keys.Contains(text))
                jsonFile = JsonFile<T>.pool[text];
            else
            {
                jsonFile = new JsonFile<T>(path, filename, encoding, impersonateLogin, impersonatePassword);
                JsonFile<T>.pool.Add(text, jsonFile);
            }
            return jsonFile;
        }

        private void ProcessAttributes(DbHeader dbHeader, T instance)
        {
            PropertyInfo[] properties = GetPublicProperties(typeof(T)).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo propertyInfo = properties[i];
                var customAttributes = propertyInfo.GetCustomAttributes(true).ToArray();
                for (int j = 0; j < customAttributes.Length; j++)
                {
                    if (customAttributes[j] is PrimaryKeyAttribute)
                    {
                        if (propertyInfo.PropertyType == typeof(short) || propertyInfo.PropertyType == typeof(int) || propertyInfo.PropertyType == typeof(long))
                        {
                            PropertyInfo arg_9E_0 = propertyInfo;
                            object arg_9E_1 = instance;
                            int num = dbHeader.idMax + 1;
                            dbHeader.idMax = num;
                            arg_9E_0.SetValue(arg_9E_1, num);
                        }
                        else if (propertyInfo.PropertyType == typeof(string))
                        {
                            propertyInfo.SetValue(instance, Guid.NewGuid().ToString());
                        }
                    }
                }
            }
        }

        private static List<PropertyInfo> GetPublicProperties(Type type)
        {
            if (type.GetTypeInfo().IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetTypeInfo().ImplementedInterfaces)
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = GetTypesPublicProperties(subType);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos;
            }

            var list = new List<PropertyInfo>();

            foreach (var t in GetTypesPublicProperties(type))
            {
                if (t.GetIndexParameters().Length == 0)
                {
                    list.Add(t);
                }
            }
            return list;
        }

        private static PropertyInfo[] GetTypesPublicProperties(Type subType)
        {
            var pis = new List<PropertyInfo>();
            foreach (var pi in subType.GetRuntimeProperties())
            {
                var mi = pi.GetMethod ?? pi.SetMethod;
                if (mi != null && mi.IsStatic) continue;
                pis.Add(pi);
            }
            return pis.ToArray();
        }

        private JsonFile(string path, string filename, Encoding encoding = null, string impersonateLogin = null, string impersonatePassword = null)
        {
            this.path = path;
            this.filename = filename;
            this.encoding = encoding;
            this.file = File.GetInstance(path, filename, encoding, impersonateLogin, impersonatePassword);
        }

        public int Insert(T item)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                foo = FooHelper.Create<T>();

            this.ProcessAttributes(foo._header, item);
            foo.data.Add(item);
            this.file.Save<T>(foo);
            return 1;
        }

        public int Insert(List<T> list)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                foo = FooHelper.Create<T>();

            foreach (T current in list)
            {
                this.ProcessAttributes(foo._header, current);
                foo.data.Add(current);
            }
            this.file.Save<T>(foo);
            return 1;
        }

        public int Delete(Predicate<T> lambda)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null || foo.data.Count <= 0)
                return 0;

            int arg_3D_0 = foo.data.RemoveAll(lambda);
            this.file.Save<T>(foo.data);
            return arg_3D_0;
        }
        
        public bool DeleteFile()
        {
            return this.file.DeleteFile();
        }

        public bool ChangeHeader(string version, string title, string descriptor)
        {
            Foo<T> foo = null;
            try
            {
                foo = this.file.Read<T>();
            }
            catch (Exception ex)
            {
                foo = null;
            }
            if (foo == null)
            {
                foo = FooHelper.Create<T>();
                foo.data = new List<T>();
            }
            foo._header.version = version;
            foo._header.title = title;
            foo._header.descriptor = descriptor;
            this.file.Save<T>(foo);
            return true;
        }

        public bool CheckVersion(string version)
        {
            Foo<T> foo = null;
            try
            {
                foo = this.file.Read<T>();
            }
            catch (Exception ex)
            {
                return false;
            }
            if (foo == null)
            {
                return false;
            }
            if (string.Compare(foo._header.version, version, true) == 0)
                return true;

            return false;
        }

        public int Update(Predicate<T> lambda, Action<T> action)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                return 0;

            List<T> list = foo.data.FindAll(lambda);
            if (list == null || list.Count <= 0)
                return 0;

            foreach (T current in list)
            {
                action(current);
            }
            this.file.Save<T>(foo.data);
            return list.Count;
        }

        public List<T> StartUpdate()
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                foo = FooHelper.Create<T>();
            return foo.data;
        }

        public List<T> UpdateData(List<T> items, Predicate<T> lamdba, Action<T> action)
        {
            List<T> list = items.FindAll(lamdba);
            if (list == null || list.Count <= 0)
                return items;

            foreach (T current in list)
            {
                action(current);
            }

            return items;
        }

        public bool Commit(List<T> items)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                foo = FooHelper.Create<T>();
            foo.data = items;
            //this.file.Save<T>(foo.data);
            this.file.Save<T>(foo);
            return true;
        }

        public List<T> Select()
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                return new List<T>();

            return foo.data;
        }        

        public List<T> Select(Predicate<T> lambda = null)
        {
            Foo<T> foo = this.file.Read<T>();
            if (foo == null)
                return new List<T>();

            if (lambda == null)
                return new List<T>();

            return foo.data.FindAll(lambda);
        }

        public int Reset(List<T> list)
        {
            this.file.Save<T>(list);
            return list.Count;
        }

        public void Dispose()
        {
        }

        ~JsonFile()
        {
            this.Dispose();
        }
    }
}
