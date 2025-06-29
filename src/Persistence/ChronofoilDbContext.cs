using Chronofoil.CaptureFile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chronofoil.Web.Persistence;

public class ChronofoilDbContext : DbContext
{
    public DbSet<User> Users { get; private set; }
    public DbSet<CfTokenInfo> CfTokens { get; private set; }
    public DbSet<RemoteTokenInfo> RemoteTokens { get; private set; }
    public DbSet<ChronofoilUpload> Uploads { get; private set; }
    public DbSet<CensoredOpcode> Opcodes { get; private set; }

    private readonly IConfiguration _config;

    public ChronofoilDbContext() { }
    
    public ChronofoilDbContext(DbContextOptions<ChronofoilDbContext> options, IConfiguration config)
        : base(options)
    {
        _config = config;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(_config["CF_CONNSTRING"]);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(x => x.CfUserId);

        modelBuilder.Entity<RemoteTokenInfo>()
            .HasKey(x => x.TokenId);

        modelBuilder.Entity<RemoteTokenInfo>()
            .HasIndex(x => x.UserId);
        
        modelBuilder.Entity<CfTokenInfo>()
            .HasKey(x => x.TokenId);
        
        modelBuilder.Entity<CfTokenInfo>()
            .HasIndex(x => x.RefreshToken);
        
        modelBuilder.Entity<ChronofoilUpload>()
            .HasKey(x => x.CfCaptureId);

        modelBuilder.Entity<CensoredOpcode>()
            .HasKey(x => new { x.GameVersion, x.Key });
    }

    public void PostMigrate()
    {
        // SaveChanges();
    }
}