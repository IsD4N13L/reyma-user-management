using MediatR;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Mappings;
using UserManagement.Domain.Users.Services;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Features
{
    public static class AddUser
    {
        public sealed record Command(UserForCreationDto UserToAdd) : IRequest<UserDto>;
        public sealed class Handler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<Command, UserDto>
        {
            public async Task<UserDto> Handle(Command request, CancellationToken cancellationToken)
            {
                var userToAdd = request.UserToAdd.ToUserForCreation();
                var user = User.Create(userToAdd);

                await userRepository.Add(user, cancellationToken);
                await unitOfWork.CommitChanges(cancellationToken);

                return user.ToUserDto();
            }
        }
    }
}
