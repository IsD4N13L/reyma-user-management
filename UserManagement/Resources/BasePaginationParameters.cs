namespace UserManagement.Resources
{
    public abstract  class BasePaginationParameters
    {
        internal virtual int MaxPageSize { get; } = 50000;
        internal virtual int DefaultPageSize { get; set; } = 10;

        public virtual int PageNumber { get; set; } = 1;

        public string? Grouping { get; set; }

        public int PageSize
        {
            get
            {
                return DefaultPageSize;
            }
            set
            {
                DefaultPageSize = value > MaxPageSize ? MaxPageSize : value;
            }
        }
    }
}
