using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class WorkflowTemplateViewModel
{
    public int WorkflowTemplateId { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(150)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}

public class WorkflowStepViewModel
{
    public int WorkflowStepId { get; set; }
    public int WorkflowTemplateId { get; set; }

    [Required(ErrorMessage = "El nombre del paso es requerido")]
    [StringLength(150)]
    [Display(Name = "Nombre del paso")]
    public string StepName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Orden")]
    [Range(1, 99)]
    public int StepOrder { get; set; }

    [Display(Name = "Tipo de asignación")]
    public string AssignmentType { get; set; } = "role";

    [Display(Name = "Usuario asignado")]
    public string? AssignedUserId { get; set; }

    [Display(Name = "Rol asignado")]
    public string? AssignedRoleName { get; set; }

    [Display(Name = "Requiere todos los aprobadores")]
    public bool RequiresAllApprovers { get; set; }

    public IEnumerable<SelectListItem> Users { get; set; } = [];
    public IEnumerable<SelectListItem> Roles { get; set; } = [];
}

public class WorkflowTemplateStepsViewModel
{
    public int WorkflowTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public bool IsActive { get; set; }
    public List<WorkflowStepRowViewModel> Steps { get; set; } = [];
    public WorkflowStepViewModel NewStep { get; set; } = new();
}

public class WorkflowStepRowViewModel
{
    public int WorkflowStepId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssignedDisplay { get; set; }
    public bool RequiresAllApprovers { get; set; }
}
