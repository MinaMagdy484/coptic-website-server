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
    public class WordMeaningsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WordMeaningsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: WordMeanings
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.WordMeanings.Include(w => w.Meaning).Include(w => w.Word);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: WordMeanings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaning = await _context.WordMeanings
                .Include(w => w.Meaning)
                .Include(w => w.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordMeaning == null)
            {
                return NotFound();
            }

            return View(wordMeaning);
        }

        // GET: WordMeanings/Create
        public IActionResult Create()
        {
            ViewData["MeaningID"] = new SelectList(_context.Meanings, "ID", "MeaningText");
            ViewData["WordID"] = new SelectList(_context.Words
                .Select(w => new {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }), "WordId", "DisplayField");
            return View();
        }

        // POST: WordMeanings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,WordID,MeaningID")] WordMeaning wordMeaning)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wordMeaning);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MeaningID"] = new SelectList(_context.Meanings, "ID", "ID", wordMeaning.MeaningID);
            //ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", wordMeaning.WordID);
            ViewData["WordID"] = new SelectList(_context.Words
                .Select(w => new {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }), "WordId", "DisplayField");
            return View(wordMeaning);
        }

        // GET: WordMeanings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaning = await _context.WordMeanings.FindAsync(id);
            if (wordMeaning == null)
            {
                return NotFound();
            }
            ViewData["MeaningID"] = new SelectList(_context.Meanings, "ID", "ID", wordMeaning.MeaningID);
            //ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", wordMeaning.WordID);
            ViewData["WordID"] = new SelectList(_context.Words
            .Select(w => new {
            w.WordId,
            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
            }), "WordId", "DisplayField", wordMeaning.WordID);
            return View(wordMeaning);
        }

        // POST: WordMeanings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,WordID,MeaningID")] WordMeaning wordMeaning)
        {
            if (id != wordMeaning.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(wordMeaning);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WordMeaningExists(wordMeaning.ID))
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
            ViewData["MeaningID"] = new SelectList(_context.Meanings, "ID", "ID", wordMeaning.MeaningID);
            ViewData["WordID"] = new SelectList(_context.Words
                            .Select(w => new {
                                w.WordId,
                                DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                            }), "WordId", "DisplayField");
            return View(wordMeaning);
        }

        // GET: WordMeanings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordMeaning = await _context.WordMeanings
                .Include(w => w.Meaning)
                .Include(w => w.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordMeaning == null)
            {
                return NotFound();
            }

            return View(wordMeaning);
        }

        // POST: WordMeanings/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var wordMeaning = await _context.WordMeanings.FindAsync(id);
        //    if (wordMeaning != null)
        //    {
        //        _context.WordMeanings.Remove(wordMeaning);
        //    }

        //    await _context.SaveChangesAsync();
        //    return View(nameof(Index));
        //}
        // POST: Word_Meaning/Delete/5
        // POST: Word_Meaning/Delete/5
        // POST: Word_Meaning/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl)
        {
            var wordMeaning = await _context.WordMeanings.FindAsync(id);
            if (wordMeaning != null)
            {
                _context.WordMeanings.Remove(wordMeaning);
                await _context.SaveChangesAsync();
            }

            // Redirect back to the same page using the returnUrl if it's provided
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Default to Index if no returnUrl is available
            return RedirectToAction(nameof(Index));
        }



        private bool WordMeaningExists(int id)
        {
            return _context.WordMeanings.Any(e => e.ID == id);
        }
    }
}
