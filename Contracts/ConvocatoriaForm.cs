using System.ComponentModel.DataAnnotations;

namespace Delta.Contracts;

public class ConvocatoriaForm : IValidatableObject
{
    public bool Publicada { get; set; } = true;
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Ingrese el título."), StringLength(255, ErrorMessage = "Máximo 255 caracteres.")]
    public string NombreConvocatoria { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string EntidadEmisora { get; set; } = "Laboratorios Delta S.A.";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string Resumen { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Descripcion { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Objetivo { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Participantes { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Requisitos { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Modalidad { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    private string cronograma = "";
    public string Cronograma
    {
        get => FechaEvaluacion.HasValue && HoraInicio.HasValue && HoraFin.HasValue
            ? $"Fecha: {FechaEvaluacion:yyyy-MM-dd} | Horario: {HoraInicio:HH:mm} - {HoraFin:HH:mm} (Bolivia)" : cronograma;
        set => cronograma = value ?? "";
    }
    public DateOnly? FechaEvaluacion { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public bool CargarCronograma()
    {
        var m = System.Text.RegularExpressions.Regex.Match(cronograma,
            @"^Fecha: (\d{4}-\d{2}-\d{2}) \| Horario: (\d{2}:\d{2}) - (\d{2}:\d{2}) \(Bolivia\)$");
        if (!m.Success || !DateOnly.TryParseExact(m.Groups[1].Value,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var fecha)
            || !TimeOnly.TryParseExact(m.Groups[2].Value,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var inicio)
            || !TimeOnly.TryParseExact(m.Groups[3].Value,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var fin)) return false;
        FechaEvaluacion=fecha; HoraInicio=inicio; HoraFin=fin; return true;
    }
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(10000, ErrorMessage = "Máximo 10000 caracteres.")]
    public string Instrucciones { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string Lugar { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string Contacto { get; set; } = "";
    [Required(ErrorMessage = "Complete este campo obligatorio."), RegularExpression("^(Activa|Cerrada)$", ErrorMessage = "Seleccione Activa o Cerrada.")]
    public string EstadoConvocatoria { get; set; } = "Activa";
    [Required(ErrorMessage = "Ingrese la fecha de publicación.")]
    public DateOnly? FechaConvocatoria { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "Ingrese la fecha límite.")]
    public DateOnly? FechaCierre { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaEvaluacion.HasValue || HoraInicio.HasValue || HoraFin.HasValue)
        {
            if (!FechaEvaluacion.HasValue || !HoraInicio.HasValue || !HoraFin.HasValue)
                yield return new ValidationResult("Seleccione fecha, hora de inicio y hora de fin de la evaluación.");
            if (HoraFin <= HoraInicio)
                yield return new ValidationResult("La hora de fin debe ser posterior a la de inicio.", new[] { nameof(HoraFin) });
            if (FechaEvaluacion < FechaConvocatoria)
                yield return new ValidationResult("La evaluación no puede ser anterior a la publicación.", new[] { nameof(FechaEvaluacion) });
        }
        if (FechaConvocatoria == DateOnly.MinValue || FechaCierre == DateOnly.MinValue)
            yield return new ValidationResult("Ingrese fechas válidas.", new[] { nameof(FechaConvocatoria), nameof(FechaCierre) });
        if (FechaCierre < FechaConvocatoria)
            yield return new ValidationResult("La fecha límite debe ser igual o posterior a la publicación.", new[] { nameof(FechaCierre) });
    }
}

public sealed class ConvocatoriaResumen
{
    public int IdConvocatoria { get; set; }
    public string Codigo { get; set; } = "";
    public string NombreConvocatoria { get; set; } = "";
    public string Resumen { get; set; } = "";
    public string EstadoConvocatoria { get; set; } = "";
    public DateOnly? FechaConvocatoria { get; set; }
    public DateOnly? FechaCierre { get; set; }
}

public sealed class ConvocatoriaGestion
{
    public int IdConvocatoria { get; set; }
    public string Codigo { get; set; } = "";
    public string NombreConvocatoria { get; set; } = "";
    public string EstadoConvocatoria { get; set; } = "";
    public DateOnly? FechaConvocatoria { get; set; }
    public DateOnly? FechaCierre { get; set; }
    public bool PdfPublicoDisponible { get; set; }
    public string Publicacion { get; set; } = "";
}
