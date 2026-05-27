using Microsoft.AspNetCore.Mvc.Rendering;
using QualityDMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class AuditViewModel
{
    public int AuditId { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50)]
    [Display(Name = "Código")]
    public string AuditCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El departamento es requerido")]
    [Display(Name = "Departamento")]
    public int DepartmentId { get; set; }

    [Display(Name = "Estado")]
    public AuditStatus Status { get; set; } = AuditStatus.Planned;

    [Required(ErrorMessage = "La fecha planificada es requerida")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha planificada")]
    public DateTime PlannedDate { get; set; } = DateTime.Today.AddDays(7);

    [DataType(DataType.Date)]
    [Display(Name = "Fecha ejecutada")]
    public DateTime? ExecutedDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de cierre")]
    public DateTime? ClosedDate { get; set; }

    [StringLength(2000)]
    [Display(Name = "Resumen")]
    public string? Summary { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = [];
}

public class AuditFindingViewModel
{
    public int FindingId { get; set; }
    public int AuditId { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tipo")]
    public string FindingType { get; set; } = "NC";

    [StringLength(1000)]
    [Display(Name = "Acción correctiva")]
    public string? CorrectiveAction { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha límite")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Cerrado")]
    public bool IsClosed { get; set; }
}
