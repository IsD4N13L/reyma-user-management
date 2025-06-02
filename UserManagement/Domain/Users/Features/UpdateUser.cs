using MediatR;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Mappings;
using UserManagement.Domain.Users.Services;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Features
{
    public static class UpdateUser
    {
        public sealed record Command(Guid UserId, UserForUpdateDto UpdatedUserData) : IRequest;

        public sealed class Handler(IUserRepository userRepository, IUnitOfWork unitOfWork)
            : IRequestHandler<Command>
        {
            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var userToUpdate = await userRepository.GetById(request.UserId, cancellationToken: cancellationToken);
                var userToAdd = request.UpdatedUserData.ToUserForUpdate();
                userToUpdate.Update(userToAdd);

                userRepository.Update(userToUpdate);
                await unitOfWork.CommitChanges(cancellationToken);
            }
        }
    }
}
