namespace PlataformaCreditos.ViewModels;

public class SolicitudFilterViewModel
{
    public string? Estado { get; set; }
    public decimal? MinMonto { get; set; }
    public decimal? MaxMonto { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
