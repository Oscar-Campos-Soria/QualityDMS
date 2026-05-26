using Microsoft.AspNetCore.Mvc.Rendering;
using QualityDMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class CreateDocumentViewModel
{
    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50)]
    [Display(Name = "Código")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "La categoría es requerida")]
    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "El departamento es requerido")]
    [Display(Name = "Departamento")]
    public int DepartmentId { get; set; }

    [Display(Name = "Plantilla de flujo")]
    public int? WorkflowTemplateId { get; set; }

    [Display(Name = "Fecha próxima revisión")]
    [DataType(DataType.Date)]
    public DateTime? NextReviewDate { get; set; }

    [Required(ErrorMessage = "El archivo es requerido")]
    [Display(Name = "Archivo")]
    public IFormFile? File { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IEnumerable<SelectListItem> Departments { get; set; } = [];
    public IEnumerable<SelectListItem> WorkflowTemplates { get; set; } = [];
}

public class EditDocumentViewModel
{
    public int DocumentId { get; set; }
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Departamento")]
    public int DepartmentId { get; set; }

    [Display(Name = "Plantilla de flujo")]
    public int? WorkflowTemplateId { get; set; }

    [Display(Name = "Fecha próxima revisión")]
    [DataType(DataType.Date)]
    public DateTime? NextReviewDate { get; set; }

    [Display(Name = "Número de versión")]
    public string? NewVersionNumber { get; set; }

    [Display(Name = "Registro de cambios")]
    [StringLength(1000)]
    public string? ChangeLog { get; set; }

    [Display(Name = "Nuevo archivo (opcional)")]
    public IFormFile? File { get; set; }

    public DocumentStatus CurrentStatus { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IEnumerable<SelectListItem> Departments { get; set; } = [];
    public IEnumerable<SelectListItem> WorkflowTemplates { get; set; } = [];
}

public class DocumentFilterViewModel
{
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IEnumerable<SelectListItem> Departments { get; set; } = [];
}
