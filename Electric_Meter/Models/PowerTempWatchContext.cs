using Microsoft.EntityFrameworkCore;

namespace Electric_Meter.Models;

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



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {


        OnModelCreatingPartial(modelBuilder);
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
