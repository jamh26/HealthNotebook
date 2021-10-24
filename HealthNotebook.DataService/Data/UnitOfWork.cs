using System;
using System.Threading.Tasks;
using HealthNotebook.DataService.IConfiguration;
using HealthNotebook.DataService.IRepository;
using HealthNotebook.DataService.Repository;
using Microsoft.Extensions.Logging;

namespace HealthNotebook.DataService.Data
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;

        public IUsersRepository Users { get; private set; }
        public IRefreshTokensRepository RefreshTokens { get; private set; }

        public UnitOfWork(AppDbContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger("db_logs");

            Users = new UsersRepository(context, _logger);
            RefreshTokens = new RefreshTokensRepository(context, _logger);
        }

        public async Task CompleteAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}