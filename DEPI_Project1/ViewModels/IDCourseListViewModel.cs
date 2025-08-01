using Microsoft.AspNetCore.Mvc.Rendering;

namespace DEPI_Project1.ViewModels
{
    public class IDCourseListViewModel
    {
        public int CourseID { get; set; }
        public SelectList Courses { get; set; }
    }
}
