using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Electric_Meter_WebAPI.Models
{
    public class PowerTempWatchContextFactory : IDesignTimeDbContextFactory<PowerTempWatchContext>
    {
        public PowerTempWatchContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetSection("AppSettings:ConnectString").Value;

            var optionsBuilder = new DbContextOptionsBuilder<PowerTempWatchContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new PowerTempWatchContext(optionsBuilder.Options);
        }
    }
}
