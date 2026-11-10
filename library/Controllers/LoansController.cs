using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace library.Controllers;

// Answers every address that starts with /Loans.
public class LoansController : Controller
{
    private readonly LoanRepository _loans;
    private readonly MemberRepository _members;
    private readonly BookRepository _books;

    public LoansController(LoanRepository loans, MemberRepository members, BookRepository books)
    {
        _loans = loans;
        _members = members;
        _books = books;
    }

    // GET /Loans : every loan, newest first.
    public async Task<IActionResult> Index()
    {
        var loans = await _loans.GetAllAsync();
        return View(loans);
    }

    // Fills the Borrow form's two dropdowns from the database.
    private async Task FillDropdownsAsync()
    {
        ViewBag.Members = new SelectList(await _members.GetAllAsync(), "MemberId", "FullName");
        ViewBag.Books = new SelectList(await _books.GetAllAsync(), "BookId", "Title");
    }

    // GET /Loans/Borrow : the empty Borrow form.
    public async Task<IActionResult> Borrow()
    {
        await FillDropdownsAsync();
        return View(new BorrowForm());
    }

    // POST /Loans/Borrow : the form was submitted.[cite: 5]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowForm form)
    {
        if (ModelState.IsValid) // 1. every rule on the form was kept
        {
            // 2. Try to borrow the book, and keep the answer.[cite: 5]
            var success = await _loans.BorrowAsync(form.MemberId!.Value, form.BookId!.Value, form.DaysAllowed);
            if (success) // 3. the loan was saved
            {
                // 4. Go back to the list of loans.[cite: 5]
                return RedirectToAction(nameof(Index));
            }
            // 5. Explain why nothing was saved.[cite: 5]
            ModelState.AddModelError("", $"This member already has {LoanRepository.MaxOpenLoans} books out.");
        }

        // 6. Fill both dropdowns again.[cite: 5]
        ViewBag.Books = new SelectList(await _books.GetAllAsync(), "BookId", "Title", form.BookId);
        ViewBag.Members = new SelectList(await _members.GetAllAsync(), "MemberId", "FullName", form.MemberId);
        // 7. Show the form again, with what was chosen.[cite: 5]
        return View(form);
    }

    // POST /Loans/Return/5 : the Return button was pressed.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(long id)
    {
        await _loans.ReturnAsync(id);
        return RedirectToAction(nameof(Index));
    }

}
