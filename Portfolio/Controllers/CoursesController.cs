using Microsoft.AspNetCore.Mvc;
using portfolio.Repositories;
using portfolio.ViewModels;

namespace portfolio.Controllers;

/// <summary>Viewer side, read-only: the courses I have taken and each one's projects.</summary>
public class CoursesController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;

    public CoursesController(CourseRepository courses, ProjectRepository projects)
    {
        _courses = courses;
        _projects = projects;
    }

    // GET /Courses?q=web
    public async Task<IActionResult> Index(string? q)
    {
        ViewData["Search"] = q;
        return View(await _courses.GetAllAsync(q));
    }

    // GET /Courses/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound(); // → friendly 404 page
        }

        return View(new CourseDetailsViewModel
        {
            Course = course,
            Projects = await _projects.GetAllAsync(courseId: id)
        });
    }
}
