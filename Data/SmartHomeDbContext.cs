using Microsoft.EntityFrameworkCore;
using SmartHomeIoT.Api.Models;

namespace SmartHomeIoT.Api.Data;

public class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------
        // Room
        // ---------------------------------------------------------------
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room");
            entity.HasKey(r => r.RoomId);
            entity.Property(r => r.RoomId).HasColumnName("RoomID");
            entity.Property(r => r.Name).HasColumnName("Name").IsRequired().HasMaxLength(100);
            entity.HasIndex(r => r.Name);
        });

        // ---------------------------------------------------------------
        // Device
        // ---------------------------------------------------------------
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Device");
            entity.HasKey(d => d.DeviceId);
            entity.Property(d => d.DeviceId).HasColumnName("DeviceID");
            entity.Property(d => d.Name).HasColumnName("Name").IsRequired().HasMaxLength(100);
            entity.Property(d => d.Type).HasColumnName("Type").IsRequired().HasMaxLength(50);
            entity.Property(d => d.RoomId).HasColumnName("RoomID");
            entity.Property(d => d.MacAddress).HasColumnName("MACAddress").IsRequired().HasMaxLength(17);
            entity.Property(d => d.IPv4Address).HasColumnName("IPv4Address").HasMaxLength(45);
            entity.Property(d => d.Status).HasColumnName("Status").HasConversion<string>().HasMaxLength(20);
            entity.Property(d => d.RegistrationDate).HasColumnName("RegistrationDate");
            entity.Property(d => d.LastSeen).HasColumnName("LastSeen");

            entity.HasIndex(d => d.MacAddress).IsUnique();

            entity.HasOne(d => d.Room)
                  .WithMany(r => r.Devices)
                  .HasForeignKey(d => d.RoomId)
                  .OnDelete(DeleteBehavior.Restrict); // a room with devices can't be deleted (see RoomsController)
        });

        // ---------------------------------------------------------------
        // SensorData
        // ---------------------------------------------------------------
        modelBuilder.Entity<SensorData>(entity =>
        {
            entity.ToTable("SensorData");
            entity.HasKey(s => s.DataId);
            entity.Property(s => s.DataId).HasColumnName("DataID");
            entity.Property(s => s.DeviceId).HasColumnName("DeviceID");
            entity.Property(s => s.SensorType).HasColumnName("SensorType").IsRequired().HasMaxLength(50);
            entity.Property(s => s.Value).HasColumnName("Value").HasColumnType("decimal(8,2)");
            entity.Property(s => s.Unit).HasColumnName("Unit").IsRequired().HasMaxLength(10);
            entity.Property(s => s.Timestamp).HasColumnName("Timestamp");

            // Requirement D-08: composite index on (DeviceID, Timestamp) so history queries stay fast at ~1M rows.
            entity.HasIndex(s => new { s.DeviceId, s.Timestamp });

            entity.HasOne(s => s.Device)
                  .WithMany(d => d.SensorReadings)
                  .HasForeignKey(s => s.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade); // measurement history has no meaning without the device
        });

        // ---------------------------------------------------------------
        // EventLog
        // ---------------------------------------------------------------
        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.ToTable("EventLog");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.Event).HasColumnName("Event").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasColumnName("Timestamp");

            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.Device)
                  .WithMany(d => d.Events)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull); // keep the historical event even after the device is removed
        });
    }
}
