using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthNotebook.DataService.Data;
using HealthNotebook.DataService.IRepository;
using HealthNotebook.Entities.DbSet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthNotebook.DataService.Repository
{
    public class RefreshTokensRepository : GenericRepository<RefreshToken>, IRefreshTokensRepository
    {
        public RefreshTokensRepository(
            AppDbContext context,
            ILogger logger
            ) : base(context, logger)
        {

        }

        public override async Task<IEnumerable<RefreshToken>> All()
        {
            try
            {
                return await dbSet.Where(x => x.Status == 1)
                                .AsNoTracking()
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} All method has generated an error", typeof(RefreshTokensRepository));
                return new List<RefreshToken>();
            }
        }

        public async Task<RefreshToken> GetByRefreshToken(string refreshToken)
        {
            try
            {
                return await dbSet.Where(x => x.Token == refreshToken)
                                .AsNoTracking()
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetByRefreshToken method has generated an error", typeof(RefreshTokensRepository));
                return null;
            }
        }

        public async Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken)
        {
            try
            {
                var token = await dbSet.Where(x => x.Token == refreshToken.Token)
                                .AsNoTracking()
                                .FirstOrDefaultAsync();

                if (token == null)
                    return false;

                token.IsUsed = refreshToken.IsUsed;
                dbSet.Update(token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} MarkRefreshTokenAsUsed method has generated an error", typeof(RefreshTokensRepository));
                return false;
            }
        }
    }
}