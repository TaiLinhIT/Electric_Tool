using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IDbContextFactory
    {
        PowerTempWatchContext CreateDbContext();
        string GetCurrentConnectionString();
    }
}
