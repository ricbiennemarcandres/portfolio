using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using portfolio.Models;
using portfolio.Repositories;
using portfolio.ViewModels;

namespace portfolio.Controllers;

/// <summary>Viewer side: home page, plus the friendly error and 404 pages.</summary>
public class HomeController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<HomeController> _logger;

    public HomeController(CourseRepository courses, ProjectRepository projects,
                          ProjectImageRepository images, IWebHostEnvironment env,
                          ILogger<HomeController> logger)
    {
        _courses = courses;
        _projects = projects;
        _images = images;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            Courses = await _courses.GetAllAsync(),
            LatestProjects = await _projects.GetAllAsync(limit: 3),
            ProjectCount = await _projects.CountAsync(),
            ImageCount = await _images.CountAsync()
        };
        return View(model);
    }

    /// <summary>Friendly page for anything unexpected (set up by UseExceptionHandler in Program.cs).</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is not null)
        {
            _logger.LogError(feature.Error, "Unexpected error on {Path}", feature.Path);
        }

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Path = feature?.Path,
            Detail = _env.IsDevelopment() ? feature?.Error.Message : null
        });
    }

    /// <summary>Friendly page for 404 and other status codes (UseStatusCodePagesWithReExecute).</summary>
    public IActionResult Status(int id)
    {
        var code = id is >= 400 and <= 599 ? id : 404;
        Response.StatusCode = code;
        return View(code);
    }
}
