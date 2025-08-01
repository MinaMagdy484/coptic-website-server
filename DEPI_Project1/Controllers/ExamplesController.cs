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
    public class ExamplesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamplesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Examples
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Examples.Include(e => e.ParentExample).Include(e => e.WordMeaning);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Examples/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var example = await _context.Examples
                .Include(e => e.ParentExample)
                .Include(e => e.WordMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (example == null)
            {
                return NotFound();
            }

            return View(example);
        }

        // GET: Examples/Create
        public IActionResult Create()
        {
            ViewData["ParentExampleID"] = new SelectList(_context.Examples, "ID", "ID");
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID");
            return View();
        }

        // POST: Examples/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,ExampleText,Reference,Pronunciation,Notes,Language,WordMeaningID,ParentExampleID")] Example example)
        {
            if (ModelState.IsValid)
            {
                _context.Add(example);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentExampleID"] = new SelectList(_context.Examples, "ID", "ID", example.ParentExampleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", example.WordMeaningID);
            return View(example);
        }

        // GET: Examples/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var example = await _context.Examples.FindAsync(id);
            if (example == null)
            {
                return NotFound();
            }
            ViewData["ParentExampleID"] = new SelectList(_context.Examples, "ID", "ID", example.ParentExampleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", example.WordMeaningID);
            return View(example);
        }

        // POST: Examples/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ExampleText,Reference,Pronunciation,Notes,Language,WordMeaningID,ParentExampleID")] Example example)
        {
            if (id != example.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(example);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExampleExists(example.ID))
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
            ViewData["ParentExampleID"] = new SelectList(_context.Examples, "ID", "ID", example.ParentExampleID);
            ViewData["WordMeaningID"] = new SelectList(_context.WordMeanings, "ID", "ID", example.WordMeaningID);
            return View(example);
        }

        // GET: Examples/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var example = await _context.Examples
                .Include(e => e.ParentExample)
                .Include(e => e.WordMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (example == null)
            {
                return NotFound();
            }

            return View(example);
        }

        // POST: Examples/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var example = await _context.Examples.FindAsync(id);
            if (example != null)
            {
                _context.Examples.Remove(example);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ExampleExists(int id)
        {
            return _context.Examples.Any(e => e.ID == id);
        }
    }
}
