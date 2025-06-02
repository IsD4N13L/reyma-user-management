using UserManagement.Resources;

namespace UserManagement.Domain.Users.Dtos
{
    public sealed class UserParametersDto : BasePaginationParameters
    {
        public string? Filters { get; set; }
        public string? SortOrder { get; set; }
    }
}
