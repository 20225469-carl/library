using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;

namespace library.Controllers;

public class NotesController : Controller
{
    private readonly NoteRepository _notes;

    // ASP.NET Core hands in the repository registered in Program.cs
    public NotesController(NoteRepository notes)
    {
        _notes = notes;
    }

    // GET /Notes : the list of notes
    public async Task<IActionResult> Index()
    {
        var notes = await _notes.GetAllAsync();
        return View(notes); // Returns Views/Notes/Index.cshtml
    }

    // GET /Notes/Create : empty form
    public IActionResult Create()
    {
        return View(new Note());
    }

    // POST /Notes/Create : submit new note
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Note note)
    {
        if (!ModelState.IsValid)
        {
            return View(note);
        }

        await _notes.AddAsync(note);
        return RedirectToAction(nameof(Index));
    }

    // GET /Notes/Edit/5 : form filled with note 5
    public async Task<IActionResult> Edit(long id)
    {
        var note = await _notes.GetByIdAsync(id);
        if (note == null)
        {
            return NotFound();
        }
        return View(note);
    }

    // POST /Notes/Edit/5 : save updated note
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Note note)
    {
        if (!ModelState.IsValid)
        {
            return View(note);
        }

        await _notes.UpdateAsync(note);
        return RedirectToAction(nameof(Index));
    }

    // GET /Notes/Delete/5 : ask for confirmation
    public async Task<IActionResult> Delete(long id)
    {
        var note = await _notes.GetByIdAsync(id);
        if (note == null)
        {
            return NotFound();
        }
        return View(note);
    }

    // POST /Notes/Delete/5 : confirmed deletion
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        await _notes.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}