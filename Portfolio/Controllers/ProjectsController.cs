using Microsoft.AspNetCore.Mvc;
using portfolio.Repositories;
using portfolio.ViewModels;

namespace portfolio.Controllers;

/// <summary>Viewer side, read-only: all projects (with search) and one project's images and video.</summary>
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly CourseRepository _courses;

    public ProjectsController(ProjectRepository projects, CourseRepository courses)
    {
        _projects = projects;
        _courses = courses;
    }

    // GET /Projects?q=supabase&courseId=1
    public async Task<IActionResult> Index(string? q, int? courseId)
    {
        return View(new ProjectListViewModel
        {
            Projects = await _projects.GetAllAsync(q, courseId),
            Courses = await _courses.GetAllAsync(),
            Search = q,
            CourseId = courseId
        });
    }

    // GET /Projects/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project is null)
        {
            return NotFound(); // → friendly 404 page
        }

        return View(project);
    }
}
