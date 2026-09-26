using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.ViewModels;

namespace PlataformaCreditos.Controllers;

public class SolicitudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SolicitudController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(SolicitudFilterViewModel? filters = null)
    {
        // Get current user's client
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        if (cliente == null) return NotFound("Cliente no encontrado.");

        // Start with client's applications
        var query = _context.SolicitudesCredito
            .Where(s => s.ClienteId == cliente.Id)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filters?.Estado) && filters.Estado != "Todos")
        {
            if (Enum.TryParse<EstadoSolicitud>(filters.Estado, out var estado))
            {
                query = query.Where(s => s.Estado == estado);
            }
        }

        if (filters?.MinMonto.HasValue == true && filters.MinMonto > 0)
        {
            query = query.Where(s => s.MontoSolicitado >= filters.MinMonto);
        }

        if (filters?.MaxMonto.HasValue == true && filters.MaxMonto > 0)
        {
            query = query.Where(s => s.MontoSolicitado <= filters.MaxMonto);
        }

        if (filters?.FechaInicio.HasValue == true)
        {
            query = query.Where(s => s.FechaSolicitud >= filters.FechaInicio.Value);
        }

        if (filters?.FechaFin.HasValue == true)
        {
            query = query.Where(s => s.FechaSolicitud <= filters.FechaFin.Value);
        }

        var solicitudes = await query.ToListAsync();

        var viewModel = solicitudes.Select(s => new SolicitudListViewModel
        {
            Id = s.Id,
            MontoSolicitado = s.MontoSolicitado,
            FechaSolicitud = s.FechaSolicitud,
            Estado = s.Estado.ToString(),
            MotivoRechazo = s.MotivoRechazo
        }).ToList();

        // Pass filters back to view
        ViewBag.Filters = filters ?? new SolicitudFilterViewModel();
        return View(viewModel);
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
}
