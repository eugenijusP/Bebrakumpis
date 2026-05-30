using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.DTOs;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Houses.Queries;

public record GetAllHousesQuery : IRequest<Result<IEnumerable<HouseResponse>>>;

public class GetAllHousesQueryHandler(IHouseRepository houseRepository)
    : IRequestHandler<GetAllHousesQuery, Result<IEnumerable<HouseResponse>>>
{
    public async Task<Result<IEnumerable<HouseResponse>>> HandleAsync(GetAllHousesQuery query, CancellationToken ct)
    {
        var houses = await houseRepository.GetAllAsync(ct);
        var response = houses.Select(h => new HouseResponse
        {
            Id = h.Id,
            Name = h.Name,
            BookingColor = h.BookingColor,
            CreatedAt = h.CreatedAt
        });
        return Result<IEnumerable<HouseResponse>>.Success(response);
    }
}
