namespace BigsData.Database
{
    public class OperationResult
    {
        protected OperationResult(bool success = true, DatabaseException exception = null)
        {
            Success = success;
            Exception = exception;
        }

        public bool Success { get; private set; }
        public DatabaseException Exception { get; private set; }

        internal static OperationResult Successful { get { return new OperationResult(); } }

        internal static OperationResult Failed(DatabaseException exception)
        {
            return new OperationResult(false, exception);
        }

        public static implicit operator bool(OperationResult result)
        {
            return result.Success;
        }

        public static implicit operator DatabaseException(OperationResult result)
        {
            return result.Exception;
        }
    }

    public class ItemOperationResult : OperationResult
    {
        public ItemOperationResult(string id, bool success = true, DatabaseException exception = null)
            : base(success, exception)
        {
            ItemId = id;
        }

        public string ItemId { get; private set; }

        internal static ItemOperationResult Sucessful(string id)
        {
            return new ItemOperationResult(id);
        }

        public static new ItemOperationResult Failed(DatabaseException ex)
        {
            return new ItemOperationResult(null, false, ex);
        }
    }
}
