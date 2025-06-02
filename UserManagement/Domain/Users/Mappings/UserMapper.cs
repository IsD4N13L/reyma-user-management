using Riok.Mapperly.Abstractions;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Models;

namespace UserManagement.Domain.Users.Mappings
{
    [Mapper]
    public static partial class UserMapper
    {
        public static partial UserForCreation ToUserForCreation(this UserForCreationDto userForCreationDto);
        public static partial UserForUpdate ToUserForUpdate(this UserForUpdateDto userForUpdateDto);
        public static partial UserDto ToUserDto(this User user);
        public static partial IQueryable<UserDto> ToUserDtoQueryable(this IQueryable<User> user);
    }
}
