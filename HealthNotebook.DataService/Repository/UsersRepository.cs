using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthNotebook.DataService.Data;
using HealthNotebook.DataService.IRepository;
using HealthNotebook.Entities.DbSet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthNotebook.DataService.Repository;

public class UsersRepository : GenericRepository<User>, IUsersRepository
{
    public UsersRepository(
        AppDbContext context,
        ILogger logger
        ) : base(context, logger)
    {

    }

    /// <summary>
    /// Task to query the DB for all users with active status
    /// </summary>
    /// <returns>Returns all users found in DB</returns>
    public override async Task<IEnumerable<User>> All()
    {
        try
        {
            return await dbSet.Where(x => x.Status == 1)
                            .AsNoTracking()
                            .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} All method has generated an error", typeof(UsersRepository));
            return new List<User>();
        }
    }

    /// <summary>
    /// Task to update a user profile in the DB
    /// </summary>
    /// <param name="user">User object the method will update</param>
    /// <returns>boolean flag indicating whether method completed successfully or not</returns>
    public async Task<bool> UpdateUserProfile(User user)
    {
        try
        {
            var existingUser = await dbSet.Where(x => x.Status == 1
                                                && x.Id == user.Id)
                            .FirstOrDefaultAsync();

            if (existingUser == null) return false; //no user exists

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.MobileNumber = user.MobileNumber;
            existingUser.Phone = user.Phone;
            existingUser.Gender = user.Gender;
            existingUser.Address = user.Address;
            existingUser.UpdateDate = DateTime.UtcNow;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateUserProfile method has generated an error", typeof(UsersRepository));
            return false;
        }
    }

    public async Task<User> GetByIdentityId(Guid identityId)
    {
        try
        {
            return await dbSet.Where(x => x.Status == 1
                                                && x.IdentityId == identityId)
                            .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetByIdentityId method has generated an error", typeof(UsersRepository));
            return null;
        }
    }
}