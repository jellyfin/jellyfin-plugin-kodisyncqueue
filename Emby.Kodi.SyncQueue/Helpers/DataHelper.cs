using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Library;
using System.IO;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;


namespace Emby.Kodi.SyncQueue.Helpers
{
    class DataHelper : IDisposable
    {
        private SQLiteConnection dbConn = null;
        private ILogger _logger;
        private IJsonSerializer _jsonSerializer;

        private bool bNeedDisposed;
        private string dbPath;
        private readonly object _dbConnSyncLock = new object();
       

        public DataHelper(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            bNeedDisposed = false;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        /*
        ~DataHelper()
        {
            lock (_dbConnSyncLock)
            {
                if (bNeedDisposed)
                {
                    if (dbConn != null && dbConn.State == ConnectionState.Open)
                        try
                        {
                            dbConn.Close();
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorException(e.Message, e);
                        }
                }
            }
        }
        */

        public void CheckCreateFiles(string embyDataPath)
        {
            lock (_dbConnSyncLock)
            {
                dbPath = Path.Combine(embyDataPath, "SyncData", "Emby.Kodi.SyncQueue.db");

                if (!Directory.Exists(Path.Combine(embyDataPath, "SyncData")))
                {
                    _logger.Debug("Emby.Kodi.SyncQueue:  Creating Sync Data Folder...");
                    Directory.CreateDirectory(Path.Combine(embyDataPath, "SyncData"));
                }

                if (!File.Exists(dbPath))
                {
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Creating database file: {0}", dbPath));
                    SQLiteConnection.CreateFile(dbPath);
                }
            }            
        }

        public bool TableExists(string tableName)
        {
            var sSQL = String.Format("SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{0}'", tableName);
            using (SQLiteDataReader dReader = SQLReader(sSQL))
            {
                if (dReader != null)
                {
                    if (dReader.HasRows)
                    {
                        dReader.Close();
                        return true;
                    }
                    else
                    {
                        _logger.Debug(String.Format("Table not found: {0}... Creating table...", tableName));
                        dReader.Close();
                        return false;
                    }
                }
                else { return false; }                
            }
        }

        
        public void OpenConnection()
        {
            lock (_dbConnSyncLock)
            {
                dbConn = new SQLiteConnection("Data Source=" + dbPath + ";Version=3");
                dbConn.Open();
                bNeedDisposed = true;
            }
        }

        public void CreateLibraryTable(string tableName, string indexName)
        {
            string sSQL;
            int i;

            if (!this.TableExists(tableName))
            {
                sSQL = String.Format("CREATE TABLE IF NOT EXISTS {0} (itemId VARCHAR(50), userId VARCHAR(50), lastModified DATETIME)", tableName);
                _logger.Info(String.Format("Emby.Kodi.SyncQueue: Creating Table {0}", tableName));
                SQLExecuter(sSQL, out i);
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Table Created:  {0}", tableName));
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue: Creating Index {0}", indexName));
                sSQL = String.Format("CREATE UNIQUE INDEX IF NOT EXISTS {0} ON {1} (userId, itemId)", indexName, tableName);
                SQLExecuter(sSQL, out i);
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Index Created: {0}  For Table:  {1}", indexName, tableName));
            }
        }

        public void CreateUserTable(string tableName, string indexName)
        {
            string sSQL;
            int i;

            if (!this.TableExists(tableName))
            {
                sSQL = String.Format("CREATE TABLE IF NOT EXISTS {0} (itemId VARCHAR(50), userId VARCHAR(50), json TEXT, lastModified DATETIME)", tableName);
                _logger.Info(String.Format("Emby.Kodi.SyncQueue: Creating Table {0}", tableName));
                SQLExecuter(sSQL, out i);
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Table Created:  {0}", tableName));
                sSQL = String.Format("CREATE UNIQUE INDEX IF NOT EXISTS {0} ON {1} (userId, itemId)", indexName, tableName);
                _logger.Info(String.Format("Emby.Kodi.SyncQueue: Creating Index {0}", indexName));
                SQLExecuter(sSQL, out i);
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Index Created: {0}  For Table:  {1}", indexName, tableName));
            }            
        }

        public void SQLExecuter(string sSQL, out int recChanged)
        {            
            using (var command = new SQLiteCommand(sSQL, dbConn))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Executing SQL Statement:  {0}", sSQL));
                //lock (_dbConnSyncLock) { recChanged = command.ExecuteNonQuery(); };
                recChanged = command.ExecuteNonQuery();
            }            
        }

        public SQLiteDataReader SQLReader(string sSQL)
        {
            using (var command = new SQLiteCommand(sSQL, dbConn))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Reader SQL Statement:  {0}", sSQL));
                return command.ExecuteReader();                
            }
        }

        public async Task<SQLiteDataReader> SQLReaderAsync(string sSQL)
        {
            using (var command = new SQLiteCommand(sSQL, dbConn))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue: ReaderAsync SQL Statement:  {0}", sSQL));
                return (SQLiteDataReader)await command.ExecuteReaderAsync();
            }
        }

        public async Task<string> LibrarySetItemAsync(string item, string user, string tableName, ILibraryManager _libraryManager, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sSQL = String.Format("INSERT OR REPLACE INTO {0} VALUES ('{1}','{2}','{3}')", tableName, item, user, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Adding ItemId: '{0}' for UserID: '{1}' to table: '{2}'", item, user, tableName));
            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement '{0}'", sSQL));
            using (var _command = new SQLiteCommand(sSQL, dbConn))
            {
                await _command.ExecuteNonQueryAsync();
                //return String.Format("{0}({1}-{2})", item, GetParentString(item, _libraryManager), _libraryManager.GetItemById(item).GetClientTypeName());
                return String.Format("{0}({1})", item, GetParentString(item, _libraryManager));
            }           
        }

        public async Task<string> UserChangeSetItemAsync(MediaBrowser.Model.Dto.UserItemDataDto dto, string user, string item, string tableName, ILibraryManager _libraryManager, CancellationToken cancellationToken)
        {
            var _json = _jsonSerializer.SerializeToString(dto).ToString();

            var sSQL = String.Format("INSERT OR REPLACE INTO {0} VALUES ('{1}','{2}','{3}','{4}')", tableName, item, user, _json, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

            using (var _command = new SQLiteCommand(sSQL, dbConn))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Updating ItemId: '{0}' for UserId: '{1}' in table: '{2}'", item, user, tableName));
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement: '{0}'", sSQL));
                await _command.ExecuteNonQueryAsync(); 
                //return String.Format("{0}({1}-{2})", item, GetParentString(item, _libraryManager), _libraryManager.GetItemById(item).GetClientTypeName());
                return String.Format("{0}({1})", item, GetParentString(item, _libraryManager));
            }            
        }

        private string GetParentString(string itemId, ILibraryManager _libraryManager)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item != null)
            {
                var sName = item.Name;
                if (item.Parent != null)
                {
                    var ctn = item.Parent.GetClientTypeName();
                    if (ctn == null || ctn == "AggregateFolder" || ctn == "Folder")
                    {
                        return sName;
                    }
                    else
                    {
                        var res = GetParentString(item.Parent.Id.ToString(), _libraryManager);
                        return String.Format("{0}, {1}", res, sName);
                    }
                }
                else { return sName; }
            } else { return ""; }
        }

        public async Task<List<string>> FillItemsAddedAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsAddedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " +
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();

            using (var _reader = await SQLReaderAsync(sSQL))
            {

                while (_reader.Read())
                {
                    info.Add(_reader["itemId"].ToString());
                }
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Added Items Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Added Items Found!");                
                return info;
            }
        }

        public async Task<List<string>> FillItemsUpdatedAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsUpdatedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) AND NOT EXISTS " +
                "( SELECT c.itemId FROM ItemsAddedQueue c WHERE c.lastModified >= '{0}' AND c.userId = '{1}' AND c.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            using (var _reader = await SQLReaderAsync(sSQL))
            {
                while (_reader.Read())
                {
                    info.Add(_reader["itemId"].ToString());
                }
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Updated Items Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Updated Items Found!");
                return info;
            }
        }

        public async Task<List<string>> FillItemsRemovedAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsRemovedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsAddedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " +
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            using (var _reader = await SQLReaderAsync(sSQL))
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Removed Items Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Removed Items Found!");
                return info;
            }
        }

        public async Task<List<string>> FillFoldersAddedToAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM FoldersAddedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            using (var _reader = await SQLReaderAsync(sSQL))
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Added Folders Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Added Folders Found!");
                return info;
            }
        }

        public async Task<List<string>> FillFoldersRemovedFromAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM FoldersRemovedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersAddedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            using (var _reader = await SQLReaderAsync(sSQL))
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Removed Folders Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Removed Folders Found!");
                return info;
            }
        }

        public async Task<List<string>> FillUserDataChangedAsync(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId, a.json FROM UserInfoChangedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersRemovedQueue b WHERE b.lastModified > a.lastModified AND b.userId = '{1}' AND b.itemId = a.itemId AND NOT EXISTS " +
                "( SELECT c.itemId FROM FoldersAddedQueue c WHERE c.lastModified > b.lastModified AND c.userId = '{1}' AND c.itemId = a.itemId ) ) " + 
                "ORDER BY lastModified", lastDT, userId);
            var info = new List<string>();
            var info2 = new List<string>();
            using (var _reader = await SQLReaderAsync(sSQL))
            {
                while (_reader.Read())
                {
                    info.Add(_reader["json"].ToString());
                    info2.Add(_reader["itemId"].ToString());
                }
                _reader.Close();
                if (info2.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  User Items Found: {0}", string.Join(",", info2.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No User Items Found!");
                info2.Clear();
                return info;
            }
        }

        public async Task<int> RetentionFixerAsync(string tableName, DateTime retDate)
        {
            int recChanged = 0;
            string sSQL = String.Format("DELETE FROM {0} WHERE lastModified >= '{1}'", tableName, retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            using (var _command = new SQLiteCommand(sSQL, dbConn))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue.Task: Retention Deletion From Table '{0}' Using SQL Statement '{1}'", tableName, sSQL));
                _command.CommandText = sSQL;
                recChanged = await _command.ExecuteNonQueryAsync();
                return recChanged;
            }
        }

        public async Task<List<String>> RetentionTablesAsync()
        {
            string sSQL = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1";
            List<String> tables = new List<String>();
            using (SQLiteDataReader rdr = await SQLReaderAsync(sSQL))
            {
                using (DataTable dt = new DataTable())
                {
                    dt.Load(rdr);
                    foreach (DataRow row in dt.Rows)
                    {
                        tables.Add(row.ItemArray[0].ToString());
                    }
                    rdr.Close();
                }
            }
            return tables;

        }        

        public async Task CleanupDatabaseAsync()
        {
            String sql = "vacuum;";
            using (var cmd = new SQLiteCommand(sql, dbConn))
            {
                _logger.Info("Emby.Kodi.SyncQUeue: Beginning Vacuum");
                await cmd.ExecuteNonQueryAsync();
                _logger.Info("Emby.Kodi.SyncQueue: Vacuum Finished!");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (bNeedDisposed)
                {
                    if (dbConn != null && dbConn.State == ConnectionState.Open)
                    {
                        dbConn.Close();
                        dbConn = null;                        
                    }
                }
            }
        }
    }
}
