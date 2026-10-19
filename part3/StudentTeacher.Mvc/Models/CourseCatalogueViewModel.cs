namespace StudentTeacher.Mvc.Models;

// Displays courses and the available filter choices on one page.
public class CourseCatalogueViewModel
{
    public int? SelectedCategoryId { get; set; }
    public List<CourseViewModel> Courses { get; set; } = new();
    public List<CourseCategoryOption> Categories { get; set; } = new();
}
