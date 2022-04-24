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

public class HealthDataRepository
 : GenericRepository<HealthData>, IHealthDataRepository
{
    public HealthDataRepository(
        AppDbContext context,
        ILogger logger
        ) : base(context, logger)
    {

    }

    /// <summary>
    /// Task to query the DB for all HealthDatas with active status
    /// </summary>
    /// <returns>Returns all HealthDatas found in DB</returns>
    public override async Task<IEnumerable<HealthData>> All()
    {
        try
        {
            return await dbSet.Where(x => x.Status == 1)
                            .AsNoTracking()
                            .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} All method has generated an error", typeof(HealthDataRepository));
            return new List<HealthData>();
        }
    }

    /// <summary>
    /// Task to update a HealthData profile in the DB
    /// </summary>
    /// <param name="HealthData">HealthData object the method will update</param>
    /// <returns>boolean flag indicating whether method completed successfully or not</returns>
    public async Task<bool> UpdateHealthData(HealthData HealthData)
    {
        try
        {
            var existingHealthData = await dbSet.Where(x => x.Status == 1
                                                && x.Id == HealthData.Id)
                            .FirstOrDefaultAsync();

            if (existingHealthData == null) return false; //no HealthData exists

            existingHealthData.BloodType = HealthData.BloodType;
            existingHealthData.Height = HealthData.Height;
            existingHealthData.Race = HealthData.Race;
            existingHealthData.Weight = HealthData.Weight;
            existingHealthData.UseGlasses = HealthData.UseGlasses;
            existingHealthData.IsSmoker = HealthData.IsSmoker;
            existingHealthData.UpdateDate = DateTime.UtcNow;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateHealthData method has generated an error", typeof(HealthDataRepository));
            return false;
        }
    }
}