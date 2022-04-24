using System.Threading.Tasks;
using HealthNotebook.DataService.IRepository;

namespace HealthNotebook.DataService.IConfiguration;

public interface IUnitOfWork
{
    IUsersRepository Users { get; }
    IRefreshTokensRepository RefreshTokens { get; }
    IHealthDataRepository HealthData { get; }
    Task CompleteAsync();
}
