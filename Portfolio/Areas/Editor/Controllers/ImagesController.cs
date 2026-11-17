using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;
using portfolio.Repositories;

namespace portfolio.Areas.Editor.Controllers;

/// <summary>
/// Editor side: add, edit and delete ONE image of a project.
/// (To replace all of a project's images at once, use the project's Edit form.)
/// </summary>
[Area("Editor")]
[Authorize]
public class ImagesController : Controller
{
    private readonly ProjectImageRepository _images;
    private readonly ProjectRepository _projects;

    public ImagesController(ProjectImageRepository images, ProjectRepository projects)
    {
        _images = images;
        _projects = projects;
    }

    // GET /Editor/Images?projectId=5
    public async Task<IActionResult> Index(int projectId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        return project is null ? NotFound() : View(project);
    }

    // GET /Editor/Images/Create?projectId=5
    public async Task<IActionResult> Create(int projectId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project is null)
        {
            return NotFound();
        }

        return View(new ProjectImage
        {
            ProjectId = projectId,
            ProjectTitle = project.Title,
            SortOrder = await _images.NextSortOrderAsync(projectId)
        });
    }

    // POST /Editor/Images/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectImage image)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(image);
        }

        try
        {
            await _images.AddAsync(image);
            TempData["Success"] = "The image was added.";
            return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            // e.g. 23505 uq_project_image_url, 23503 fk_project_image_project
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Message(ex));
            return await RedisplayAsync(image);
        }
    }

    // GET /Editor/Images/Edit/7
    public async Task<IActionResult> Edit(int id)
    {
        var image = await _images.GetByIdAsync(id);
        return image is null ? NotFound() : View(image);
    }

    // POST /Editor/Images/Edit/7
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectImage image)
    {
        if (id != image.ImageId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(image);
        }

        try
        {
            if (!await _images.UpdateAsync(image))
            {
                return NotFound();
            }

            TempData["Success"] = "The image was saved.";
            return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Message(ex));
            return await RedisplayAsync(image);
        }
    }

    // GET /Editor/Images/Delete/7
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _images.GetByIdAsync(id);
        return image is null ? NotFound() : View(image);
    }

    // POST /Editor/Images/Delete/7
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var image = await _images.GetByIdAsync(id);
        if (image is null)
        {
            return NotFound();
        }

        try
        {
            await _images.DeleteAsync(id);
            TempData["Success"] = "The image was deleted.";
            return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(string.Empty, DbErrors.DeleteMessage(ex));
            return View(image);
        }
    }

    // Shows the form again with the project's title filled in.
    private async Task<IActionResult> RedisplayAsync(ProjectImage image)
    {
        var project = await _projects.GetByIdAsync(image.ProjectId);
        if (project is null)
        {
            return NotFound();
        }

        image.ProjectTitle = project.Title;
        return View(image);
    }
}
