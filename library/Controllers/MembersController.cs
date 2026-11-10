using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace library.Controllers;

public class MembersController : Controller
{
    private readonly MemberRepository _members;
    private readonly LoanRepository _loans;

    // ASP.NET Core hands in the repositories, because Program.cs registered them.[cite: 7]
    public MembersController(MemberRepository members, LoanRepository loans)
    {
        _members = members;
        _loans = loans;
    }

    public async Task<IActionResult> Index()
    {
        var members = await _members.GetAllAsync();
        return View(members);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Member member)
    {
        if (!ModelState.IsValid)
        {
            return View(member);
        }

        await _members.CreateAsync(member);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    public async Task<IActionResult> Delete(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // POST /Members/Delete/5 : the delete was confirmed.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            // 1. Delete the member.
            await _members.DeleteAsync(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // 3. Load the member again, to show them on the page.
            var member = await _members.GetByIdAsync(id);
            if (member == null)
            {
                // 5. Answer "not found".
                return NotFound();
            }
            // 6. Set the message the page will show.
            ViewBag.Error = "This member has loans on record, so the database will not delete them.";
            // 7. Show the Delete page again, with the member.
            return View("Delete", member);
        }

        // 8. The member was deleted: go back to the list.
        return RedirectToAction(nameof(Index));
    }

    // GET /Members/Details/5 : one member and their loans.[cite: 8]
    public async Task<IActionResult> Details(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) 
        {
            return NotFound();
        }
        var details = new MemberDetails
        {
            Member = member,
            Loans = await _loans.GetByMemberAsync(id)
        };
        return View(details);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(long id, Member member)
    {
        if (id != member.MemberId) return NotFound();

        if (!ModelState.IsValid) return View(member);

        await _members.UpdateAsync(member);
        return RedirectToAction(nameof(Index));
    }
}