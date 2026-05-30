using Bebrakumpis.Domain.Entities;

namespace Bebrakumpis.Domain.Interfaces;

public interface IHouseRepository
{
    Task<IEnumerable<House>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<House?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(House house, CancellationToken cancellationToken = default);
    Task UpdateAsync(House house, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOtherAsync(string name, Guid excludeId, CancellationToken cancellationToken = default);
    Task<bool> HasBookingsAsync(Guid id, CancellationToken cancellationToken = default);
}
