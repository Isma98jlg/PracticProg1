using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AnalistaController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        return View(solicitudes);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        var cliente = solicitud.Cliente;
        var maxMonto = cliente.IngresosMensuales * 5;
        var excedeLimite = solicitud.MontoSolicitado > maxMonto;

        var viewModel = new AnalistaSolicitudViewModel
        {
            Id = solicitud.Id,
            ClienteNombre = cliente.UsuarioId,
            MontoSolicitado = solicitud.MontoSolicitado,
            IngresosMensuales = cliente.IngresosMensuales,
            MaxAprobacion = maxMonto,
            ExcedeLimite = excedeLimite,
            FechaSolicitud = solicitud.FechaSolicitud,
            Estado = solicitud.Estado.ToString(),
            MotivoRechazo = solicitud.MotivoRechazo
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        // No aprobar si ya fue aprobada o rechazada
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Esta solicitud ya fue procesada.";
            return RedirectToAction("Index");
        }

        // No aprobar si el monto excede 5 veces los ingresos
        var maxMonto = solicitud.Cliente.IngresosMensuales * 5;
        if (solicitud.MontoSolicitado > maxMonto)
        {
            TempData["Error"] = $"No se puede aprobar: el monto ({solicitud.MontoSolicitado:C}) excede 5 veces los ingresos ({maxMonto:C}).";
            return RedirectToAction("Detail", new { id });
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Solicitud #{solicitud.Id} aprobada exitosamente.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string motivo)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        // No rechazar si ya fue aprobada o rechazada
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Esta solicitud ya fue procesada.";
            return RedirectToAction("Index");
        }

        // Motivo obligatorio en rechazo
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["Error"] = "El motivo de rechazo es obligatorio.";
            return RedirectToAction("Detail", new { id });
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivo;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Solicitud #{solicitud.Id} rechazada.";
        return RedirectToAction("Index");
    }
}

public class AnalistaSolicitudViewModel
{
    public int Id { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal MontoSolicitado { get; set; }
    public decimal IngresosMensuales { get; set; }
    public decimal MaxAprobacion { get; set; }
    public bool ExcedeLimite { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
}
