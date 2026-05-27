using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.DTOs;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Users.Queries;

public record GetAllUsersQuery : IRequest<Result<IEnumerable<UserResponse>>>;

public class GetAllUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserResponse>>>
{
    public async Task<Result<IEnumerable<UserResponse>>> HandleAsync(GetAllUsersQuery query, CancellationToken ct)
    {
        var users = await userRepository.GetAllAsync(ct);
        return Result<IEnumerable<UserResponse>>.Success(users.Select(u => new UserResponse
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }));
    }
}
