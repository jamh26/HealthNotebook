using System.Threading.Tasks;
using HealthNotebook.Entities.DbSet;

namespace HealthNotebook.DataService.IRepository
{
    public interface IRefreshTokensRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByRefreshToken(string refreshToken);
        Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken);
    }
}