using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using portfolio.Data;
using portfolio.Helpers;
using portfolio.Repositories;
using portfolio.ViewModels;

namespace portfolio.Areas.Editor.Controllers;

/// <summary>
/// Editor side: add, edit and delete projects.
/// The form includes the image URLs (one per line); saving runs ONE transaction.
/// </summary>
[Area("Editor")]
[Authorize]
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly CourseRepository _courses;

    public ProjectsController(ProjectRepository projects, CourseRepository courses)
    {
        _projects = projects;
        _courses = courses;
    }

    // GET /Editor/Projects?courseId=1&q=library
    public async Task<IActionResult> Index(int? courseId, string? q)
    {
        return View(new ProjectListViewModel
        {
            Projects = await _projects.GetAllAsync(q, courseId),
            Courses = await _courses.GetAllAsync(),
            Search = q,
            CourseId = courseId
        });
    }

    // GET /Editor/Projects/Create?courseId=1
    public async Task<IActionResult> Create(int? courseId)
    {
        var model = new ProjectFormViewModel { CourseId = courseId ?? 0 };
        await FillCoursesAsync(model);
        return View(model);
    }

    // POST /Editor/Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillCoursesAsync(model);
            return View(model);
        }

        var urls = model.ImageUrlList();
        try
        {
            // ONE transaction: the project and every image, or nothing.
            await _projects.AddWithImagesAsync(model.ToProject(), urls);
            TempData["Success"] = $"“{model.Title}” was saved with {urls.Count} image(s).";
            return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ShowRefusal(ex, urls);
            await FillCoursesAsync(model);
            return View(model);
        }
    }

    // GET /Editor/Projects/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project is null)
        {
            return NotFound();
        }

        var model = ProjectFormViewModel.From(project);
        await FillCoursesAsync(model);
        return View(model);
    }

    // POST /Editor/Projects/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectFormViewModel model)
    {
        if (id != model.ProjectId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await FillCoursesAsync(model);
            return View(model);
        }

        var urls = model.ImageUrlList();
        try
        {
            // ONE transaction: update the project, replace its images.
            if (!await _projects.UpdateWithImagesAsync(model.ToProject(), urls))
            {
                return NotFound();
            }

            TempData["Success"] = $"“{model.Title}” was saved with {urls.Count} image(s).";
            return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ShowRefusal(ex, urls);
            await FillCoursesAsync(model);
            return View(model);
        }
    }

    // GET /Editor/Projects/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projects.GetByIdAsync(id);
        return project is null ? NotFound() : View(project);
    }

    // POST /Editor/Projects/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            if (!await _projects.DeleteAsync(id))
            {
                return NotFound();
            }

            TempData["Success"] = "The project and its images were deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(string.Empty, DbErrors.DeleteMessage(ex));
            var project = await _projects.GetByIdAsync(id);
            return project is null ? NotFound() : View(project);
        }
    }

    /// <summary>Puts the database's refusal under the right box, plus a note that nothing was saved.</summary>
    private void ShowRefusal(PostgresException ex, List<string> urls)
    {
        var field = DbErrors.FieldFor(ex);
        var message = DbErrors.Message(ex);

        if (field == "ImageUrl")
        {
            field = nameof(ProjectFormViewModel.ImageUrls); // the textarea on this form
        }

        if (ex.ConstraintName == "uq_project_image_url" && FindRepeat(urls) is { } repeat)
        {
            message = $"Image {repeat.Second} repeats image {repeat.First}: \"{TextHelper.Shorten(urls[repeat.Second - 1], 70)}\". " +
                      $"The same URL cannot be added twice to one project. [{ex.SqlState} · {ex.ConstraintName}]";
        }

        ModelState.AddModelError(field, message);
        ModelState.AddModelError(string.Empty,
            "Nothing was saved. The database refused one of the values, so the whole transaction " +
            "(the project and all its images) was rolled back.");
    }

    /// <summary>Finds the first URL listed twice (1-based positions), only to explain the refusal.</summary>
    private static (int First, int Second)? FindRepeat(List<string> urls)
    {
        var seen = new Dictionary<string, int>();
        for (var i = 0; i < urls.Count; i++)
        {
            if (seen.TryGetValue(urls[i], out var first))
            {
                return (first + 1, i + 1);
            }
            seen[urls[i]] = i;
        }
        return null;
    }

    private async Task FillCoursesAsync(ProjectFormViewModel model)
    {
        var courses = await _courses.GetAllAsync();
        model.Courses = courses.Select(c => new SelectListItem(
            $"{c.Code} — {c.Title}", c.CourseId.ToString(), c.CourseId == model.CourseId));
    }
}
