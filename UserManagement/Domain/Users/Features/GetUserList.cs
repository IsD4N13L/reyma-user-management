using MediatR;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Services;
using Microsoft.EntityFrameworkCore;
using QueryKit;
using QueryKit.Configuration;
using UserManagement.Domain.Users.Mappings;
using UserManagement.Resources;

namespace UserManagement.Domain.Users.Features
{
    public static class GetUserList
    {
        public sealed record Query(UserParametersDto QueryParameters) : IRequest<List<UserDto>>;

        public sealed class Handler(IUserRepository userRepository)
        : IRequestHandler<Query, List<UserDto>>
        {
            public async Task<List<UserDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var collection = userRepository.Query().AsNoTracking();

                var queryKitConfig = new CustomQueryKitConfiguration();
                var queryKitData = new QueryKitData()
                {
                    Filters = request.QueryParameters.Filters,
                    SortOrder = request.QueryParameters.SortOrder,
                    Configuration = queryKitConfig
                };
                var appliedCollection = collection.ApplyQueryKit(queryKitData);
                var dtoCollection = appliedCollection.ToUserDtoQueryable();

                return await dtoCollection.ToListAsync(cancellationToken);
            }
        }
    }
}
