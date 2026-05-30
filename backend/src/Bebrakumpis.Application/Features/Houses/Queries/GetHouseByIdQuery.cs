using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.DTOs;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Houses.Queries;

public record GetHouseByIdQuery(Guid Id) : IRequest<Result<HouseResponse>>;

public class GetHouseByIdQueryHandler(IHouseRepository houseRepository)
    : IRequestHandler<GetHouseByIdQuery, Result<HouseResponse>>
{
    public async Task<Result<HouseResponse>> HandleAsync(GetHouseByIdQuery query, CancellationToken ct)
    {
        var house = await houseRepository.GetByIdAsync(query.Id, ct);
        if (house is null)
            return Result<HouseResponse>.NotFound($"House '{query.Id}' not found.");

        return Result<HouseResponse>.Success(new HouseResponse
        {
            Id = house.Id,
            Name = house.Name,
            BookingColor = house.BookingColor,
            CreatedAt = house.CreatedAt
        });
    }
}
