using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CalidadSYS.ViewModels;

public class CategoryViewModel
{
    public int CategoryId { get; set; }

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

    [Display(Name = "Categoría padre")]
    public int? ParentCategoryId { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> ParentCategories { get; set; } = [];
}
