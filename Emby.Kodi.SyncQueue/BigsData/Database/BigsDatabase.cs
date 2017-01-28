using BigsData.Database.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigsData.Database
{
    public class BigsDatabase
    {
        #region Private fields
        private readonly string _rootFolder;
        private readonly string _defaultDatabase;
        private readonly string _defaultCollection;
        private readonly bool _failSilently;
        private readonly bool _trackReferences;
        private const string _guidFormat = "N";
        private static readonly Encoding _encoding = new UTF8Encoding(false);
        private readonly IDictionary<WeakReference, ItemReference> _references;
        #endregion

        internal BigsDatabase(string baseFolder, string defaultDatabase, string defaultCollection, bool failSilentlyOnReads, bool trackReferences)
        {
            _rootFolder = baseFolder;
            _defaultDatabase = defaultDatabase;
            _defaultCollection = defaultCollection;
            _failSilently = failSilentlyOnReads;
            _trackReferences = trackReferences;
            _references = new Dictionary<WeakReference, ItemReference>();
        }

        #region Public methods
        #region Create
        public async Task<ItemOperationResult> Add<T>(T item, string collection = null, string database = null) where T : class, new()
        {
            var id = NewId();

            return await Add(id, item, collection, database);
        }

        public async Task<ItemOperationResult> Add<T>(string id, T item, string collection = null, string database = null) where T : class, new()
        {
            return await AddInt(id, item, collection, database, SerializeItemToFile);
        }

        public async Task<ItemOperationResult> Add(Stream stream, string collection = null, string database = null)
        {
            var id = NewId();
            return await AddStream(id, stream, collection, database);
        }

        public async Task<ItemOperationResult> AddStream(string id, Stream stream, string collection = null, string database = null)
        {
            return await AddInt(id, stream, collection, database, CopyStreamToFile);
        }
        #endregion

        #region Read
        public Task<T> Single<T>(string id, string collection = null, string database = null) where T : class, new()
        {
            var itemReference = BuildItemReference(id, collection, database);

            if (!File.Exists(itemReference.RootFullPath))
                if (_failSilently)
                    return Task.FromResult(default(T));
                else
                    throw new ItemNotFoundException(itemReference.FullPath);

            return Task.FromResult(ReadItem<T>(itemReference));
        }

        public IQueryable<T> Query<T>(string collection = null, string database = null) where T : class, new()
        {
            var reference = BuildItemReference(null, collection, database);

            if (!Directory.Exists(reference.RootCollectionPath))
                if (_failSilently)
                    return new T[0].AsQueryable();

            return Directory.EnumerateFiles(reference.RootCollectionPath)
                .Select(file =>
                ReadItem<T>(
                    new ItemReference(
                        _rootFolder,
                        reference.Database,
                        reference.Collection,
                        Path.GetFileName(file)
                    ))).Where(item => default(T) != item).AsQueryable();
        }

        public Stream GetSteam(string id, string collection = null, string database = null)
        {
            var itemReference = BuildItemReference(id, collection, database);

            if (!File.Exists(itemReference.RootFullPath))
                if (_failSilently)
                    return Stream.Null;
                else
                    throw new ItemNotFoundException(itemReference.FullPath);

            return File.OpenRead(itemReference.RootFullPath);
        }
        #endregion

        #region Update
        public async Task<OperationResult> Update<T>(string id, T item, string collection = null, string database = null) where T : class, new()
        {
            var itemReference = BuildItemReference(id, collection, database);

            return await UpdateInt(itemReference, item, SerializeItemToFile);
        }

        public async Task<OperationResult> Update<T>(T item, string collection = null, string database = null) where T : class, new()
        {
            string id;

            if (TryGetId(item, out id))
                return await Update(id, item, collection, database);

            return OperationResult.Failed(new ItenNotFoundException(id));
        }

        public async Task<OperationResult> AddOrUpdate<T>(T item, string collection = null, string database = null) where T : class, new()
        {
            string id;

            if (TryGetId(item, out id))
                return await Update(id, item, collection, database);

            return await Add(item, collection, database);
        }

        public async Task<OperationResult> AddOrUpdate<T>(string id, T item, string collection = null, string database = null) where T : class, new()
        {
            var itemReference = BuildItemReference(id, collection, database);

            if (File.Exists(itemReference.RootFullPath))
                return await UpdateInt(itemReference, item, SerializeItemToFile);

            return await AddInt(itemReference, item, SerializeItemToFile);
        }

        public async Task<OperationResult> AddOrUpdateStream(string id, Stream stream, string collection = null, string database = null)
        {
            var itemReference = BuildItemReference(id, collection, database);

            if (File.Exists(itemReference.RootFullPath))
                return await UpdateInt(itemReference, stream, CopyStreamToFile);

            return await AddInt(itemReference, stream, CopyStreamToFile);
        }

        public async Task<OperationResult> UpdateStream(string id, Stream stream, string collection = null, string database = null)
        {
            var itemReference = BuildItemReference(id, collection, database);

            return await UpdateInt(itemReference, stream, CopyStreamToFile);
        }
        #endregion

        #region Delete
        public OperationResult Delete<T>(T item, string collection = null, string database = null) where T : class, new()
        {
            ItemReference itemReference;
            if (TryGetReference(item, out itemReference))
                return DeleteInt(itemReference);

            return OperationResult.Failed(new ItenNotFoundException(itemReference.FullPath));
        }

        public OperationResult Delete(string id, string collection = null, string database = null)
        {
            var itemReference = BuildItemReference(id, collection, database);

            return DeleteInt(itemReference);
        }
        #endregion

        #region Item Reference
        public bool Exists(string id, string collection = null, string database = null)
        {
            var itemReference = BuildItemReference(id, collection, database);
            return File.Exists(itemReference.RootFullPath);
        }

        public bool Exists<T>(T item, string collection = null, string database = null) where T : class, new()
        {
            string id;
            if (TryGetId(item, out id))
                return Exists(id, collection, database);
            return false;
        }

        public string GetId<T>(T item) where T : class, new()
        {
            if (!_trackReferences)
                if (_failSilently)
                    return null;
                else throw new InvalidDatabaseOperationException("References not tracked. Check contructor parameters.");

            string id;
            if (TryGetId(item, out id))
                return id;

            if (_failSilently)
                return null;
            else throw new InvalidDatabaseOperationException("Item Id not found.");
        }

        public bool TryGetId<T>(T item, out string id) where T : class, new()
        {
            var reference = _references.Keys.FirstOrDefault(wr => wr.Target == item);

            id = reference == null ? null : _references[reference].Id;

            return !string.IsNullOrEmpty(id);
        }

        public ItemReference GetReference<T>(T item) where T : class, new()
        {
            if (!_trackReferences)
                if (_failSilently)
                    return ItemReference.Emtpy;
                else throw new InvalidDatabaseOperationException("References not tracked. Check contructor parameters.");

            ItemReference reference;
            if (TryGetReference(item, out reference))
                return reference;

            if (_failSilently)
                return ItemReference.Emtpy;
            else throw new InvalidDatabaseOperationException("Item reference not found.");
        }

        public bool TryGetReference<T>(T item, out ItemReference itemReference) where T : class, new()
        {
            var reference = _references.Keys.FirstOrDefault(wr => wr.Target == item);

            itemReference = reference == null ? ItemReference.Emtpy : _references[reference];

            return !itemReference.IsEmpty;
        }
        #endregion
        #endregion

        #region Private methods
        private static string NewId()
        {
            return Guid.NewGuid().ToString(_guidFormat);
        }

        private void AddReference<T>(T item, ItemReference itemReference)
        {
            if (_trackReferences)
                _references.Add(new WeakReference(item, false), itemReference);
        }

        private OperationResult GuaranteeFileSystemStructure(ItemReference reference)
        {
            try
            {
                if (!Directory.Exists(reference.RootCollectionPath))
                    Directory.CreateDirectory(reference.RootCollectionPath);

                return OperationResult.Successful;
            }
            catch (Exception ex)
            {
                return OperationResult.Failed(new DatabaseException("Error creating database folder", ex));
            }
        }

        private async Task SerializeItemToFile<T>(FileStream fileStream, T item)
        {
            var json = Serializer.Serialize(item);
            var bytesToWrite = _encoding.GetBytes(json);
            await fileStream.WriteAsync(bytesToWrite, 0, bytesToWrite.Length);
        }

        private async Task CopyStreamToFile(FileStream fileStream, Stream stream)
        {
            await stream.CopyToAsync(fileStream);
        }

        private async Task<ItemOperationResult> AddInt<T>(string id, T item, string collection, string database, Func<FileStream, T, Task> process)
        {
            var itemReference = BuildItemReference(id, collection, database);

            return await AddInt(itemReference, item, process);
        }

        private async Task<ItemOperationResult> AddInt<T>(ItemReference itemReference, T item, Func<FileStream, T, Task> process)
        {
            if (File.Exists(itemReference.RootFullPath))
                return ItemOperationResult.Failed(new ItemAlreadyExistsException(itemReference.FullPath));

            var pathCreated = GuaranteeFileSystemStructure(itemReference);

            if (!pathCreated)
                return ItemOperationResult.Failed(pathCreated);

            try
            {
                using (var fileStream = File.OpenWrite(itemReference.RootFullPath))
                    await process(fileStream, item);

                AddReference(item, itemReference);
                return ItemOperationResult.Sucessful(itemReference.Id);
            }
            catch (Exception ex)
            {
                return ItemOperationResult.Failed(new DatabaseException($"Faled to add stream '{itemReference.FullPath}'", ex));
            }
        }

        private T ReadItem<T>(ItemReference itemReference) where T : class, new()
        {
            var json = File.ReadAllText(itemReference.RootFullPath);

            var result = Serializer.Deserialize<T>(json);
            if (default(T) != result)
                AddReference(result, itemReference);
            return result;
        }

        private async Task<OperationResult> UpdateInt<T>(ItemReference itemReference, T item, Func<FileStream, T, Task> process)
        {
            if (File.Exists(itemReference.RootFullPath))
            {
                var deleteResult = DeleteInt(itemReference);
                if (!deleteResult)
                    return deleteResult;

                return await AddInt(itemReference, item, process);

            }
            return OperationResult.Failed(new ItemNotFoundException(itemReference.FullPath));
        }

        private OperationResult DeleteInt(ItemReference itemReference)
        {
            if (itemReference.IsEmpty)
                return OperationResult.Failed(new InvalidArgumentException($"Incomplete data on Delete item: {itemReference.FullPath}"));

            if (File.Exists(itemReference.RootFullPath))
            {
                File.Delete(itemReference.RootFullPath);
                return OperationResult.Successful;
            }
            return OperationResult.Failed(new ItenNotFoundException(itemReference.FullPath));
        }

        private ItemReference BuildItemReference(string id, string collection, string database)
        {
            return new ItemReference(
                _rootFolder,
                string.IsNullOrEmpty(database) ? _defaultDatabase : database,
                string.IsNullOrEmpty(collection) ? _defaultCollection : collection,
                id);
        }
        #endregion
    }
}
