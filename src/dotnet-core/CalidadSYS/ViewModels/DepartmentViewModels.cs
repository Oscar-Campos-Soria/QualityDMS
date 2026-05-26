using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class DepartmentViewModel
{
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(20)]
    [Display(Name = "Código")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(150)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "Responsable")]
    public string? ManagerName { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}
