namespace UserManagement.Exceptions
{
    public class FileIOException : Exception
    {
        public FileIOException() : base() { }
        public FileIOException(string message) : base(message) { }
    }
}
