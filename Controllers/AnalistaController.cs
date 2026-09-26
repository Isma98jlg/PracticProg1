using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Hubs;
using PlataformaCreditos.Models;
using PlataformaCreditos.Services;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHubContext<SolicitudesHub> _hubContext;
    private readonly IRedisService _redisService;

    public AnalistaController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<SolicitudesHub> hubContext,
        IRedisService redisService)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
        _redisService = redisService;
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

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Esta solicitud ya fue procesada.";
            return RedirectToAction("Index");
        }

        var maxMonto = solicitud.Cliente.IngresosMensuales * 5;
        if (solicitud.MontoSolicitado > maxMonto)
        {
            TempData["Error"] = $"No se puede aprobar: el monto excede el límite.";
            return RedirectToAction("Detail", new { id });
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        await _context.SaveChangesAsync();

        // Invalidate Redis cache for this client's applications
        await _redisService.RemoveCachedSolicitudesAsync($"solicitudes:{solicitud.ClienteId}");

        // Emit WebSocket event to the client owner only
        var cliente = solicitud.Cliente;
        await _hubContext.Clients.Group(cliente.UsuarioId).SendAsync("SolicitudEstadoActualizado", new
        {
            SolicitudId = solicitud.Id,
            Estado = solicitud.Estado.ToString(),
            MotivoRechazo = solicitud.MotivoRechazo
        });

        TempData["Success"] = $"Solicitud #{solicitud.Id} aprobada.";
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

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Esta solicitud ya fue procesada.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["Error"] = "El motivo de rechazo es obligatorio.";
            return RedirectToAction("Detail", new { id });
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivo;
        await _context.SaveChangesAsync();

        // Invalidate Redis cache
        await _redisService.RemoveCachedSolicitudesAsync($"solicitudes:{solicitud.ClienteId}");

        // Emit WebSocket event to the client owner only
        var cliente = solicitud.Cliente;
        await _hubContext.Clients.Group(cliente.UsuarioId).SendAsync("SolicitudEstadoActualizado", new
        {
            SolicitudId = solicitud.Id,
            Estado = solicitud.Estado.ToString(),
            MotivoRechazo = solicitud.MotivoRechazo
        });

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
