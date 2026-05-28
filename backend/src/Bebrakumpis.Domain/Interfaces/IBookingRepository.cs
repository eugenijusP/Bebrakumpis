using Bebrakumpis.Domain.Entities;

namespace Bebrakumpis.Domain.Interfaces;

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
