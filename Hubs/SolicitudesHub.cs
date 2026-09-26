using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using PlataformaCreditos.Services;

namespace PlataformaCreditos.Hubs;

[Authorize]
public class SolicitudesHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisService _redisService;

    public SolicitudesHub(ApplicationDbContext context, IRedisService redisService)
    {
        _context = context;
        _redisService = redisService;
    }

    public override async Task OnConnectedAsync()
    {
        // Get user identity from server — not from browser
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("ConnectionRejected", "Authentication required.");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "anonymous");
            return;
        }

        // Add user to a group named after their user ID for targeted messaging
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await Clients.Caller.SendAsync("ConnectionAccepted", $"Connected as {userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client requests current state on reconnect to recover missed events
    /// </summary>
    public async Task RequestCurrentState(int solicitudId)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var solicitud = await _context.SolicitudesCredito
            .FirstOrDefaultAsync(s => s.Id == solicitudId);
        if (solicitud == null) return;

        // Verify the requesting user is the owner
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.UsuarioId == userId);
        if (cliente == null || solicitud.ClienteId != cliente.Id) return;

        await Clients.Caller.SendAsync("EstadoActualizado", new
        {
            SolicitudId = solicitud.Id,
            Estado = solicitud.Estado.ToString(),
            MotivoRechazo = solicitud.MotivoRechazo
        });
    }
}
