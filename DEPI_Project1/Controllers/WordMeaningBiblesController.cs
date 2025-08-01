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
    public class WordMeaningBiblesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WordMeaningBiblesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: WordMeaningBibles
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.WordMeaningBibles.Include(w => w.Bible).Include(w => w.WordMeaning);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: WordMeaningBibles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaningBible = await _context.WordMeaningBibles
                .Include(w => w.Bible)
                .Include(w => w.WordMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordMeaningBible == null)
            {
                return NotFound();
            }

            return View(wordMeaningBible);
        }

        // GET: WordMeaningBibles/Create
        public IActionResult Create()
        {
            ViewData["BibleID"] = new SelectList(_context.Bibles, "BibleID", "BibleID");
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID");
            return View();
        }

        // POST: WordMeaningBibles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,WordMeaningID,BibleID")] WordMeaningBible wordMeaningBible)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wordMeaningBible);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BibleID"] = new SelectList(_context.Bibles, "BibleID", "BibleID", wordMeaningBible.BibleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", wordMeaningBible.WordMeaningID);
            return View(wordMeaningBible);
        }

        // GET: WordMeaningBibles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaningBible = await _context.WordMeaningBibles.FindAsync(id);
            if (wordMeaningBible == null)
            {
                return NotFound();
            }
            ViewData["BibleID"] = new SelectList(_context.Bibles, "BibleID", "BibleID", wordMeaningBible.BibleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", wordMeaningBible.WordMeaningID);
            return View(wordMeaningBible);
        }

        // POST: WordMeaningBibles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,WordMeaningID,BibleID")] WordMeaningBible wordMeaningBible)
        {
            if (id != wordMeaningBible.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(wordMeaningBible);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WordMeaningBibleExists(wordMeaningBible.ID))
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
            ViewData["BibleID"] = new SelectList(_context.Bibles, "BibleID", "BibleID", wordMeaningBible.BibleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", wordMeaningBible.WordMeaningID);
            return View(wordMeaningBible);
        }

        // GET: WordMeaningBibles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaningBible = await _context.WordMeaningBibles
                .Include(w => w.Bible)
                .Include(w => w.WordMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordMeaningBible == null)
            {
                return NotFound();
            }

            return View(wordMeaningBible);
        }

        // POST: WordMeaningBibles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wordMeaningBible = await _context.WordMeaningBibles.FindAsync(id);
            if (wordMeaningBible != null)
            {
                _context.WordMeaningBibles.Remove(wordMeaningBible);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WordMeaningBibleExists(int id)
        {
            return _context.WordMeaningBibles.Any(e => e.ID == id);
        }
    }
}
