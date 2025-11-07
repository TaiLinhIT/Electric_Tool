using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Electric_Meter.Models;

public partial class PowerTempWatchContext : DbContext
{
    

    public PowerTempWatchContext(DbContextOptions<PowerTempWatchContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DvElectricDataTemp> DvElectricDataTemps { get; set; }
    public DbSet<Machine> machines { get; set; }
    public DbSet<DvFactoryAssembling> dvFactoryAssemblings { get; set; }
    public DbSet<ActiveType> activeTypes { get; set; }
    public DbSet<Controlcode> controlcodes { get; set; }
    public DbSet<devices> devices { get; set; }
    public DbSet<SensorData> sensorDatas { get; set; }
    public DbSet<SensorType> sensorTypes { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Server=10.30.201.201;Database=PowerTempWatch;TrustServerCertificate=True;User ID=sa;Password=greenland@VN;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DvElectricDataTemp>(entity =>
        {
            entity.ToTable("dv_ElectricDataTemp"); // Đặt tên bảng trong cơ sở dữ liệu
            entity.HasKey(e => e.Id); // Đặt Id làm khóa chính
        });


        OnModelCreatingPartial(modelBuilder);
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
