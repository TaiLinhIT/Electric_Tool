using Electric_Meter_WebAPI.Dto;

using Microsoft.EntityFrameworkCore;

namespace Electric_Meter_WebAPI.Models;

public partial class PowerTempWatchContext : DbContext
{


    public PowerTempWatchContext(DbContextOptions<PowerTempWatchContext> options)
        : base(options)
    {
    }


    public DbSet<ActiveType> activeTypes { get; set; }
    public DbSet<Controlcode> controlcodes { get; set; }
    public DbSet<Device> devices { get; set; }
    public DbSet<SensorData> sensorDatas { get; set; }
    public DbSet<SensorType> sensorTypes { get; set; }

    // Thêm các DbSet cho các Dto trả về từ stored procedure
    //public DbSet<LatestSensorByDeviceYear> LatestSensorByDeviceYears { get; set; }
    //public DbSet<LatestSensorDataDTO> LatestSensorData { get; set; }
    //public DbSet<DailyConsumptionDTO> DailyConsumptions { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {


        OnModelCreatingPartial(modelBuilder);
        //modelBuilder.Entity<LatestSensorByDeviceYear>().HasNoKey();
        //modelBuilder.Entity<LatestSensorDataDTO>().HasNoKey();
        //modelBuilder.Entity<DailyConsumptionDTO>().HasNoKey();
        //modelBuilder.Entity<TotalConsumptionPercentageDeviceDTO>().HasNoKey();
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

