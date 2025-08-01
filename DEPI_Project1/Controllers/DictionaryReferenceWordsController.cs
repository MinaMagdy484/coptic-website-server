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
    public class DictionaryReferenceWordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DictionaryReferenceWordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DictionaryReferenceWords
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.DictionaryReferenceWords.Include(d => d.Dictionary).Include(d => d.Word);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: DictionaryReferenceWords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dictionaryReferenceWord = await _context.DictionaryReferenceWords
                .Include(d => d.Dictionary)
                .Include(d => d.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (dictionaryReferenceWord == null)
            {
                return NotFound();
            }

            return View(dictionaryReferenceWord);
        }

        // GET: DictionaryReferenceWords/Create
        public IActionResult Create()
        {
            ViewData["DictionaryID"] = new SelectList(_context.Dictionaries, "ID", "ID");
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class");
            return View();
        }

        // POST: DictionaryReferenceWords/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,DictionaryID,WordID,Reference,Column")] DictionaryReferenceWord dictionaryReferenceWord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dictionaryReferenceWord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DictionaryID"] = new SelectList(_context.Dictionaries, "ID", "ID", dictionaryReferenceWord.DictionaryID);
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", dictionaryReferenceWord.WordID);
            return View(dictionaryReferenceWord);
        }

        // GET: DictionaryReferenceWords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dictionaryReferenceWord = await _context.DictionaryReferenceWords.FindAsync(id);
            if (dictionaryReferenceWord == null)
            {
                return NotFound();
            }
            ViewData["DictionaryID"] = new SelectList(_context.Dictionaries, "ID", "ID", dictionaryReferenceWord.DictionaryID);
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", dictionaryReferenceWord.WordID);
            return View(dictionaryReferenceWord);
        }

        // POST: DictionaryReferenceWords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,DictionaryID,WordID,Reference,Column")] DictionaryReferenceWord dictionaryReferenceWord)
        {
            if (id != dictionaryReferenceWord.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dictionaryReferenceWord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DictionaryReferenceWordExists(dictionaryReferenceWord.ID))
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
            ViewData["DictionaryID"] = new SelectList(_context.Dictionaries, "ID", "ID", dictionaryReferenceWord.DictionaryID);
            ViewData["WordID"] = new SelectList(_context.Words, "WordId", "Class", dictionaryReferenceWord.WordID);
            return View(dictionaryReferenceWord);
        }

        // GET: DictionaryReferenceWords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dictionaryReferenceWord = await _context.DictionaryReferenceWords
                .Include(d => d.Dictionary)
                .Include(d => d.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (dictionaryReferenceWord == null)
            {
                return NotFound();
            }

            return View(dictionaryReferenceWord);
        }

        // POST: DictionaryReferenceWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dictionaryReferenceWord = await _context.DictionaryReferenceWords.FindAsync(id);
            if (dictionaryReferenceWord != null)
            {
                _context.DictionaryReferenceWords.Remove(dictionaryReferenceWord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DictionaryReferenceWordExists(int id)
        {
            return _context.DictionaryReferenceWords.Any(e => e.ID == id);
        }
    }
}
