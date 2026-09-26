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

        // Seed roles
        if (!await roleManager.RoleExistsAsync("Analista"))
        {
            await roleManager.CreateAsync(new IdentityRole("Analista"));
        }

        // Seed clients only if none exist
        if (!context.Clientes.Any())
        {
            // Create users
            var clientUser1 = new IdentityUser { UserName = "client1@mail.com", Email = "client1@mail.com" };
            await userManager.CreateAsync(clientUser1, "Password123!");

            var clientUser2 = new IdentityUser { UserName = "client2@mail.com", Email = "client2@mail.com" };
            await userManager.CreateAsync(clientUser2, "Password123!");

            var analystUser = new IdentityUser { UserName = "analyst@mail.com", Email = "analyst@mail.com" };
            await userManager.CreateAsync(analystUser, "Password123!");
            await userManager.AddToRoleAsync(analystUser, "Analista");

            // Create clients
            var cliente1 = new Cliente
            {
                UsuarioId = clientUser1.Id,
                IngresosMensuales = 5000m,
                Activo = true
            };

            var cliente2 = new Cliente
            {
                UsuarioId = clientUser2.Id,
                IngresosMensuales = 8000m,
                Activo = true
            };

            context.Clientes.AddRange(cliente1, cliente2);
            await context.SaveChangesAsync();

            // Create applications with varied states for testing filters
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
                    ClienteId = cliente1.Id,
                    MontoSolicitado = 45000m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-20),
                    Estado = EstadoSolicitud.Aprobado
                },
                new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 20000m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                    Estado = EstadoSolicitud.Aprobado
                },
                new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 1500m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-15),
                    Estado = EstadoSolicitud.Rechazado,
                    MotivoRechazo = "Monto demasiado bajo para aprobación"
                },
                new SolicitudCredito
                {
                    ClienteId = cliente2.Id,
                    MontoSolicitado = 500m,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-3),
                    Estado = EstadoSolicitud.Pendiente
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
