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
    public class WordExplanationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WordExplanationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: WordExplanations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.WordExplanations.Include(w => w.Word);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: WordExplanations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordExplanation = await _context.WordExplanations
                .Include(w => w.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordExplanation == null)
            {
                return NotFound();
            }

            return View(wordExplanation);
        }

        // GET: WordExplanations/Create
        public IActionResult Create()
        {
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class");
            return View();
        }

        // POST: WordExplanations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Explanation,Language,Notes,Pronunciation,WordID")] WordExplanation wordExplanation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wordExplanation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", wordExplanation.WordID);
            return View(wordExplanation);
        }

        // GET: WordExplanations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordExplanation = await _context.WordExplanations.FindAsync(id);
            if (wordExplanation == null)
            {
                return NotFound();
            }
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", wordExplanation.WordID);
            return View(wordExplanation);
        }

        // POST: WordExplanations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Explanation,Language,Notes,Pronunciation,WordID")] WordExplanation wordExplanation)
        {
            if (id != wordExplanation.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(wordExplanation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WordExplanationExists(wordExplanation.ID))
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
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", wordExplanation.WordID);
            return View(wordExplanation);
        }

        // GET: WordExplanations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordExplanation = await _context.WordExplanations
                .Include(w => w.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordExplanation == null)
            {
                return NotFound();
            }

            return View(wordExplanation);
        }

        // POST: WordExplanations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wordExplanation = await _context.WordExplanations.FindAsync(id);
            if (wordExplanation != null)
            {
                _context.WordExplanations.Remove(wordExplanation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WordExplanationExists(int id)
        {
            return _context.WordExplanations.Any(e => e.ID == id);
        }
    }
}
