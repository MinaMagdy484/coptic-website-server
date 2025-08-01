using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CopticDictionarynew1.Controllers
{
    [Authorize(Roles = "Student,Instructor,Admin")]
    public class GroupExplanationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupExplanationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GroupExplanations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GroupExplanations.Include(g => g.GroupWord);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GroupExplanations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupExplanation = await _context.GroupExplanations
                .Include(g => g.GroupWord)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (groupExplanation == null)
            {
                return NotFound();
            }

            return View(groupExplanation);
        }

        // GET: GroupExplanations/Create
        public IActionResult Create()
        {
            ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID");
            return View();
        }

        // POST: GroupExplanations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Explanation,Language,Notes,Pronunciation,GroupID")] GroupExplanation groupExplanation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(groupExplanation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", groupExplanation.GroupID);
            return View(groupExplanation);
        }

        // GET: GroupExplanations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupExplanation = await _context.GroupExplanations.FindAsync(id);
            if (groupExplanation == null)
            {
                return NotFound();
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", groupExplanation.GroupID);
            return View(groupExplanation);
        }

        // POST: GroupExplanations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Explanation,Language,Notes,Pronunciation,GroupID")] GroupExplanation groupExplanation)
        {
            if (id != groupExplanation.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(groupExplanation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExplanationExists(groupExplanation.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", groupExplanation.GroupID);
            return View(groupExplanation);
        }

        // GET: GroupExplanations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupExplanation = await _context.GroupExplanations
                .Include(g => g.GroupWord)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (groupExplanation == null)
            {
                return NotFound();
            }

            return View(groupExplanation);
        }

        // POST: GroupExplanations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var groupExplanation = await _context.GroupExplanations.FindAsync(id);
            if (groupExplanation != null)
            {
                _context.GroupExplanations.Remove(groupExplanation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExplanationExists(int id)
        {
            return _context.GroupExplanations.Any(e => e.ID == id);
        }
    }
}
