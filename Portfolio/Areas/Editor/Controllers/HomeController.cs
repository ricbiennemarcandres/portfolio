using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using portfolio.Repositories;
using portfolio.ViewModels;

namespace portfolio.Areas.Editor.Controllers;

/// <summary>Editor dashboard. [Authorize] → signed-out visitors are sent to /Account/Login.</summary>
[Area("Editor")]
[Authorize]
public class HomeController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;

    public HomeController(CourseRepository courses, ProjectRepository projects, ProjectImageRepository images)
    {
        _courses = courses;
        _projects = projects;
        _images = images;
    }

    public async Task<IActionResult> Index()
    {
        return View(new HomeViewModel
        {
            Courses = await _courses.GetAllAsync(),
            LatestProjects = await _projects.GetAllAsync(limit: 5),
            ProjectCount = await _projects.CountAsync(),
            ImageCount = await _images.CountAsync()
        });
    }
}
