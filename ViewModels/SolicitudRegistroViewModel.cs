using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.ViewModels;

public class SolicitudRegistroViewModel
{
    [Required(ErrorMessage = "El monto solicitado es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal MontoSolicitado { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio")]
    [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
    public string Motivo { get; set; } = string.Empty;
}
