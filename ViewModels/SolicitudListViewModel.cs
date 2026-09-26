namespace PlataformaCreditos.ViewModels;

public class SolicitudListViewModel
{
    public int Id { get; set; }
    public decimal MontoSolicitado { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoLabel => Estado switch
    {
        "Pendiente" => "warning",
        "Aprobado" => "success",
        "Rechazado" => "danger",
        _ => "secondary"
    };
    public string? MotivoRechazo { get; set; }
}
