using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;

namespace library.Controllers;

public class MembersController : Controller
{
    private readonly MemberRepository _members;

    public MembersController(MemberRepository members)
    {
        _members = members;
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

    [HttpPost]
    public async Task<IActionResult> Edit(long id, Member member)
    {
        if (id != member.MemberId) return NotFound();

        if (!ModelState.IsValid) return View(member);

        await _members.UpdateAsync(member);
        return RedirectToAction(nameof(Index));
    }
}