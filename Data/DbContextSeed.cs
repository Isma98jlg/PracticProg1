using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public static class DbContextSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await context.Database.MigrateAsync();

        // Seed analyst role
        if (!await roleManager.RoleExistsAsync("Analista"))
        {
            await roleManager.CreateAsync(new IdentityRole("Analista"));
        }

        // Seed clients only if none exist
        if (!context.Clientes.Any())
        {
            var cliente1 = new Cliente
            {
                UsuarioId = "client-001",
                IngresosMensuales = 5000m,
                Activo = true
            };

            var cliente2 = new Cliente
            {
                UsuarioId = "client-002",
                IngresosMensuales = 8000m,
                Activo = true
            };

            context.Clientes.AddRange(cliente1, cliente2);
            await context.SaveChangesAsync();

            context.SolicitudesCredito.AddRange(
                new SolicitudCredito
                {
                    ClienteId = cliente1.Id,
                    MontoSolicitado = 3000m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-10),
                    Estado = EstadoSolicitud.Pendiente
                },
                new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 20000m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                    Estado = EstadoSolicitud.Aprobado
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
