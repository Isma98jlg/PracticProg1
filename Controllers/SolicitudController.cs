using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Services;
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers;

public class SolicitudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IRedisService _redisService;

    public SolicitudController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IRedisService redisService)
    {
        _context = context;
        _userManager = userManager;
        _redisService = redisService;
    }

    public async Task<IActionResult> Index(SolicitudFilterViewModel? filters = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        var cacheKey = $"solicitudes:{cliente.Id}";

        var solicitudes = await _redisService.GetCachedSolicitudesAsync(
            cacheKey,
            async () =>
            {
                var query = _context.SolicitudesCredito
                    .Where(s => s.ClienteId == cliente.Id)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(filters?.Estado) && filters.Estado != "Todos")
                {
                    if (Enum.TryParse<EstadoSolicitud>(filters.Estado, out var estado))
                    {
                        query = query.Where(s => s.Estado == estado);
                    }
                }

                if (filters?.MinMonto.HasValue == true && filters.MinMonto > 0)
                    query = query.Where(s => s.MontoSolicitado >= filters.MinMonto);

                if (filters?.MaxMonto.HasValue == true && filters.MaxMonto > 0)
                    query = query.Where(s => s.MontoSolicitado <= filters.MaxMonto);

                if (filters?.FechaInicio.HasValue == true)
                    query = query.Where(s => s.FechaSolicitud >= filters.FechaInicio.Value);

                if (filters?.FechaFin.HasValue == true)
                    query = query.Where(s => s.FechaSolicitud <= filters.FechaFin.Value);

                var list = await query.ToListAsync();
                return list.Select(s => new SolicitudListViewModel
                {
                    Id = s.Id,
                    MontoSolicitado = s.MontoSolicitado,
                    FechaSolicitud = s.FechaSolicitud,
                    Estado = s.Estado.ToString(),
                    MotivoRechazo = s.MotivoRechazo
                }).ToList();
            });

        ViewBag.Filters = filters ?? new SolicitudFilterViewModel();
        return View(solicitudes);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        if (cliente == null) return NotFound();

        var solicitud = await _context.SolicitudesCredito
            .FirstOrDefaultAsync(s => s.Id == id && s.ClienteId == cliente.Id);

        if (solicitud == null) return NotFound();

        // Track last visited solicitud in Redis session
        await _redisService.SetLastSolicitudIdAsync(user.Id, solicitud.Id);

        var viewModel = new SolicitudListViewModel
        {
            Id = solicitud.Id,
            MontoSolicitado = solicitud.MontoSolicitado,
            FechaSolicitud = solicitud.FechaSolicitud,
            Estado = solicitud.Estado.ToString(),
            MotivoRechazo = solicitud.MotivoRechazo
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        if (cliente == null) return NotFound("Cliente no encontrado.");
        if (!cliente.Activo)
        {
            TempData["Error"] = "No puedes registrar una solicitud porque tu cuenta está inactiva.";
            return RedirectToAction("Index");
        }

        var viewModel = new SolicitudRegistroViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SolicitudRegistroViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        if (!cliente.Activo)
        {
            TempData["Error"] = "No puedes registrar una solicitud porque tu cuenta está inactiva.";
            return RedirectToAction("Index");
        }

        var existingPending = await _context.SolicitudesCredito
            .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
        if (existingPending)
        {
            TempData["Error"] = "Ya tienes una solicitud pendiente.";
            return View(model);
        }

        var maxMonto = cliente.IngresosMensuales * 10;
        if (model.MontoSolicitado > maxMonto)
        {
            ModelState.AddModelError("MontoSolicitado", $"El monto no puede superar 10 veces tus ingresos ({maxMonto:C}).");
            return View(model);
        }

        var solicitud = new SolicitudCredito
        {
            ClienteId = cliente.Id,
            MontoSolicitado = model.MontoSolicitado,
            Estado = EstadoSolicitud.Pendiente,
            MotivoRechazo = null,
            FechaSolicitud = DateTime.UtcNow
        };

        _context.SolicitudesCredito.Add(solicitud);
        await _context.SaveChangesAsync();

        // Invalidate Redis cache
        await _redisService.RemoveCachedSolicitudesAsync($"solicitudes:{cliente.Id}");

        TempData["Success"] = $"¡Solicitud #{solicitud.Id} registrada! Estado: Pendiente.";
        return RedirectToAction("Index");
    }
}
