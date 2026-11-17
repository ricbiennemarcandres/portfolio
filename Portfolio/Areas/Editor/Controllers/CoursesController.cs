using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;
using portfolio.Repositories;

namespace portfolio.Areas.Editor.Controllers;

/// <summary>Editor side: add, edit and delete courses.</summary>
[Area("Editor")]
[Authorize]
public class CoursesController : Controller
{
    private readonly CourseRepository _courses;

    public CoursesController(CourseRepository courses)
    {
        _courses = courses;
    }

    // GET /Editor/Courses
    public async Task<IActionResult> Index(string? q)
    {
        ViewData["Search"] = q;
        return View(await _courses.GetAllAsync(q));
    }

    // GET /Editor/Courses/Create
    public IActionResult Create()
    {
        return View(new Course { SchoolYear = CurrentSchoolYear() });
    }

    // POST /Editor/Courses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        try
        {
            await _courses.AddAsync(course);
            TempData["Success"] = $"Course {course.Code} was added.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            // e.g. 23505 uq_course_code, 23514 ck_course_school_year → message under the box
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Message(ex));
            return View(course);
        }
    }

    // GET /Editor/Courses/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        return course is null ? NotFound() : View(course);
    }

    // POST /Editor/Courses/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Course course)
    {
        if (id != course.CourseId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(course);
        }

        try
        {
            if (!await _courses.UpdateAsync(course))
            {
                return NotFound();
            }

            TempData["Success"] = $"Course {course.Code} was saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Message(ex));
            return View(course);
        }
    }

    // GET /Editor/Courses/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        return course is null ? NotFound() : View(course);
    }

    // POST /Editor/Courses/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            if (!await _courses.DeleteAsync(id))
            {
                return NotFound();
            }

            TempData["Success"] = "The course was deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            // 23503 fk_project_course: the course still has projects (ON DELETE RESTRICT)
            ModelState.AddModelError(string.Empty, DbErrors.DeleteMessage(ex));
            var course = await _courses.GetByIdAsync(id);
            return course is null ? NotFound() : View(course);
        }
    }

    // June or later → "2026-2027"; January to May → "2025-2026"
    private static string CurrentSchoolYear()
    {
        var today = DateTime.Today;
        var start = today.Month >= 6 ? today.Year : today.Year - 1;
        return $"{start}-{start + 1}";
    }
}
