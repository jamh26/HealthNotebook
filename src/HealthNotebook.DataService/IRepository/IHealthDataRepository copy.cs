using System;
using System.Threading.Tasks;
using HealthNotebook.Entities.DbSet;

namespace HealthNotebook.DataService.IRepository;

public interface IHealthDataRepository : IGenericRepository<HealthData>
{
}
