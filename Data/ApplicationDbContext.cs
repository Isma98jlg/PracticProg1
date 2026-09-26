using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<SolicitudCredito> SolicitudesCredito => Set<SolicitudCredito>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cliente config
        builder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.IngresosMensuales)
                .HasColumnType("decimal(18,2)");
            entity.Property(c => c.Activo).HasDefaultValue(true);
            entity.HasIndex(c => c.UsuarioId).IsUnique();
        });

        // SolicitudCredito config
        builder.Entity<SolicitudCredito>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.MontoSolicitado)
                .HasColumnType("decimal(18,2)");
            entity.Property(s => s.FechaSolicitud).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(s => s.Estado).HasDefaultValue(EstadoSolicitud.Pendiente);
            entity.HasOne(s => s.Cliente)
                .WithMany(c => c.Solicitudes)
                .HasForeignKey(s => s.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
