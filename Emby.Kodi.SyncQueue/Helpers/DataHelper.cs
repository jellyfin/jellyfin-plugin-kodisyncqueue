using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.IO;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;


namespace Emby.Kodi.SyncQueue.Helpers
{
    class DataHelper
    {
        private SQLiteConnection dbConn;
        private SQLiteDataReader dReader;
        private ILogger _logger;
        private IJsonSerializer _jsonSerializer;
        //private bool bNeedDisposed;
        private string dbPath;
        private readonly object _dbConnSyncLock = new object();

        public DataHelper(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            //bNeedDisposed = false;
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

        public bool CheckCreateFiles(string embyDataPath)
        {
            try
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

                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Emby.Kodi.SyncQueue:  Error in CheckCreateFiles!");
                _logger.ErrorException(e.Message, e);
                return false;
            }
        }

        public bool TableExists(string tableName)
        {
            try
            {
                string sSQL;
                sSQL = String.Format("SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{0}'", tableName);
                if (SQLReader(sSQL, out dReader))
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
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Error creating table: {0}", tableName));
                _logger.ErrorException(e.Message, e);
                return false;
            }
        }

        
        public bool OpenConnection()
        {
            try
            {
                dbConn = new SQLiteConnection("Data Source=" + dbPath + ";Version=3");
                dbConn.Open();
                //bNeedDisposed = true;
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Could not connect to database: {0}", dbPath));
                _logger.ErrorException(e.Message, e);
                return false;
            }

        }

        public bool CreateLibraryTable(string tableName, string indexName)
        {
            string sSQL;
            bool result;
            int i;

            try
            {
                if (!this.TableExists(tableName))
                {
                    sSQL = String.Format("CREATE TABLE IF NOT EXISTS {0} (itemId VARCHAR(50), userId VARCHAR(50), lastModified DATETIME)", tableName);
                    result = SQLExecuter(sSQL, out i);
                    if (!result)
                        _logger.Error(String.Format("Could not create table: \"{0}\"", tableName));
                    else
                    {
                        _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Table Created:  {0}", tableName));
                        sSQL = String.Format("CREATE UNIQUE INDEX IF NOT EXISTS {0} ON {1} (userId, itemId)", indexName, tableName);
                        result = SQLExecuter(sSQL, out i);
                        if (!result)
                            _logger.Error(String.Format("Could not create index for \"{0}\"", tableName));
                        else
                            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Index Created: {0}  For Table:  {1}", indexName, tableName));
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Could not create table: {0} or index: {1}", tableName, indexName));
                _logger.ErrorException(e.Message, e); 
                return false;
            }
        }

        public bool CreateUserTable(string tableName, string indexName)
        {
            string sSQL;
            bool result;
            int i;

            try
            {
                if (!this.TableExists(tableName))
                {
                    sSQL = String.Format("CREATE TABLE IF NOT EXISTS {0} (itemId VARCHAR(50), userId VARCHAR(50), json TEXT, lastModified DATETIME)", tableName);
                    result = SQLExecuter(sSQL, out i);
                    if (!result)
                        _logger.Error(String.Format("Could not create table: \"{0}\"", tableName));
                    else
                    {
                        _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Table Created:  {0}", tableName));
                        sSQL = String.Format("CREATE UNIQUE INDEX IF NOT EXISTS {0} ON {1} (userId, itemId)", indexName, tableName);
                        result = SQLExecuter(sSQL, out i);
                        if (!result)
                            _logger.Error(String.Format("Could not create index for \"{0}\"", tableName));
                        else
                            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Index Created: {0}  For Table:  {1}", indexName, tableName));
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Could not create table: {0} or index: {1}", tableName, indexName));
                _logger.ErrorException(e.Message, e);
                return false;
            }
        }

        public bool SQLExecuter(string sSQL, out int recChanged)
        {
            try
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Executing SQL Statement:  {0}", sSQL));
                SQLiteCommand command = new SQLiteCommand(sSQL, dbConn);
                recChanged = command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Error executing SQL statement:  {0}", sSQL));
                _logger.ErrorException(e.Message, e);
                recChanged = 0;
                return false;
            }
        }

        public bool SQLReader(string sSQL, out SQLiteDataReader reader)
        {
            try
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Reader SQL Statement:  {0}", sSQL));
                reader = null;
                SQLiteCommand command = new SQLiteCommand(sSQL, dbConn);
                reader = command.ExecuteReader();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Error reader SQL statment:  {0}", sSQL));
                _logger.ErrorException(e.Message, e);
                reader = null;
                return false;
            }
        }

        public async Task<int> LibrarySetItem(string item, string user, string tableName, CancellationToken cancellationToken)
        {
            var recChanged = 0;
            string sSQL = String.Format("SELECT * FROM {0} WHERE itemId = '{1}' AND userId = '{2}'", tableName, item, user);
            try
            {
                SQLiteCommand _command = new SQLiteCommand(sSQL, dbConn);
                SQLiteDataReader _reader;
                _reader = _command.ExecuteReader();

                if (_reader.HasRows)
                {
                    // Must update the Value
                    sSQL = String.Format("UPDATE {0} SET lastModified = '{1:yyyy-MM-ddTHH:mm:ssZ}' WHERE itemId = '{2}' and  userId = '{3}'", tableName, DateTime.UtcNow, item, user);
                    _logger.Info(String.Format("Enby.Kdoi.SyncQueue:  Updating ItemId: '{0}' for UserId: '{1}' in table: '{2}'", item, user, tableName));
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement: '{0}'", sSQL));
                }
                else
                {
                    // Must Insert the value
                    sSQL = String.Format("INSERT INTO {0} VALUES ('{1}','{2}','{3:yyyy-MM-ddTHH:mm:ssZ}')", tableName, item, user, DateTime.UtcNow);
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Adding ItemId: '{0}' for UserID: '{1}' to table: '{2}'", item, user, tableName));
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement '{0}'", sSQL));
                }
                _reader.Close();
                _command.CommandText = sSQL;
                recChanged = await _command.ExecuteNonQueryAsync();
                return recChanged;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Error writing data to database:  '{0}'", sSQL));
                _logger.ErrorException(e.Message, e);
                return 0;
            }
        }

        public async Task<int> UserChangeSetItem(MediaBrowser.Model.Dto.UserItemDataDto dto, string user, string item, string tableName, CancellationToken cancellationToken)
        {
            var recChanged = 0;
            var _json = _jsonSerializer.SerializeToString(dto).ToString();

            string sSQL = String.Format("SELECT * FROM {0} WHERE itemId = '{1}' AND userId = '{2}'", tableName, item, user);
            try
            {
                SQLiteCommand _command = new SQLiteCommand(sSQL, dbConn);
                SQLiteDataReader _reader;
                _reader = _command.ExecuteReader();

                if (_reader.HasRows)
                {
                    // Must update the Value
                    sSQL = String.Format("UPDATE {0} SET lastModified = '{1:yyyy-MM-ddTHH:mm:ssZ}', json = '{2}' WHERE itemId = '{3}' and  userId = '{4}'", tableName, DateTime.UtcNow, _json, item, user);
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Updating ItemId: '{0}' for UserId: '{1}' in table: '{2}'", item, user, tableName));
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement: '{0}'", sSQL));
                }
                else
                {
                    // Must Insert the value
                    sSQL = String.Format("INSERT INTO {0} VALUES ('{1}','{2}','{3}','{4:yyyy-MM-ddTHH:mm:ssZ}')", tableName, item, user, _json, DateTime.UtcNow);
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Adding ItemId: '{0}' for UserID: '{1}' to table: '{2}'", item, user, tableName));
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using SQL Statement '{0}'", sSQL));
                }
                _reader.Close();
                _command.CommandText = sSQL;
                recChanged = await _command.ExecuteNonQueryAsync();
                return recChanged;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue:  Error writing data to database:  '{0}'", sSQL));
                _logger.ErrorException(e.Message, e);
                return 0;
            }
        }
        
        public async Task<int> AlterLibrary(List<string> items, string user, string tableName, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<Task<int>> LibraryAddItemQuery = 
                    from item in items select LibrarySetItem(item, user, tableName, cancellationToken);

                Task<int>[] addTasks = LibraryAddItemQuery.ToArray();
            
                int[] itemCount = await Task.WhenAll(addTasks);            
                return 1;
            }
            catch (Exception e)
            {
                _logger.Error("Emby.Kodi.SyncQueue:  Error in AlterLibrary...");
                _logger.ErrorException(e.Message, e);
                return 0;
            }
        }

        public async Task<int> LibraryItemsToAlter(List<string> items, string user, string tableName, CancellationToken cancellationToken)
        {
            try
            {
                if (items.Count > 0)
                {
                    int i = await AlterLibrary(items, user, tableName, cancellationToken);
                    return 1;
                }
                else
                {
                    return 0;
                }
                
            }
            catch (Exception e)
            {
                _logger.Error("Emby.Kodi.SyncQueue:  Error in LibraryItemsToAlter...");
                _logger.ErrorException(e.Message, e);
                return 0;
            }
        }

        public List<string> FillItemsAdded(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsAddedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " +
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
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
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No Added Items Returned Due to SQL Error!"); }
            return info;
        }

        public List<string> FillItemsUpdated(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsUpdatedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) AND NOT EXISTS " +
                "( SELECT c.itemId FROM ItemsAddedQueue c WHERE c.lastModified >= '{0}' AND c.userId = '{1}' AND c.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
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
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No Updated Items Returned Due to SQL Error!"); }
            return info;
        }

        public List<string> FillItemsRemoved(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM ItemsRemovedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM ItemsAddedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " +
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Removed Items Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Removed Items Found!");
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No Removed Items Returned Due to SQL Error!"); }
            return info;
        }

        public List<string> FillFoldersAddedTo(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM FoldersAddedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersRemovedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Added Folders Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Added Folders Found!");
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No Added Folders Returned Due to SQL Error!"); }
            return info;
        }

        public List<string> FillFoldersRemovedFrom(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId FROM FoldersRemovedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersAddedQueue b WHERE b.lastModified > a.lastModified AND a.userId = '{1}' AND b.itemId = a.itemId ) " + 
                "ORDER BY a.lastModified", lastDT, userId);
            var info = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
            {
                while (_reader.Read())
                    info.Add(_reader["itemId"].ToString());
                _reader.Close();
                if (info.Count > 0)
                    _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Removed Folders Found: {0}", string.Join(",", info.ToArray())));
                else
                    _logger.Info("Emby.Kodi.SyncQueue:  No Removed Folders Found!");
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No Removed Folders Returned Due to SQL Error!"); }
            return info;
        }

        public List<string> FillUserDataChanged(string userId, string lastDT)
        {
            var sSQL = String.Format("SELECT a.itemId, a.json FROM UserInfoChangedQueue a WHERE a.lastModified >= '{0}' AND a.userId = '{1}' AND NOT EXISTS " +
                "( SELECT b.itemId FROM FoldersRemovedQueue b WHERE b.lastModified >= a.lastModified AND b.userId = '{1}' AND b.itemId = a.itemId AND NOT EXISTS " +
                "( SELECT c.itemId FROM FoldersAddedQueue c WHERE c.lastModified > b.lastModified AND c.userId = '{1}' AND c.itemId = a.itemId ) ) " + 
                "ORDER BY lastModified", lastDT, userId);
            var info = new List<string>();
            var info2 = new List<string>();
            SQLiteDataReader _reader;

            bool result = SQLReader(sSQL, out _reader);

            if (result)
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
            }
            else { _logger.Info("Emby.Kodi.SyncQueue:  No User Items Returned Due to SQL Error!"); }
            return info;
        }

    }
}
