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
    public class MeaningsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private List<SelectListItem> GetLanguagesList()
        {
            return new List<SelectListItem>
                {
                    new SelectListItem { Value = "AR", Text = "Arabic" },
                    new SelectListItem { Value = "FR", Text = "French" },
                    new SelectListItem { Value = "EN", Text = "English" },
                    new SelectListItem { Value = "RU", Text = "Russian" },
                    new SelectListItem { Value = "DE", Text = "German" },
                    new SelectListItem { Value = "IT", Text = "Italian" },
                    new SelectListItem { Value = "HE", Text = "Hebrew" },
                    new SelectListItem { Value = "GR", Text = "Greek" },
                    new SelectListItem { Value = "ARC", Text = "Aramaic" },
                    new SelectListItem { Value = "EG",  Text = "Egyptian" },
                    new SelectListItem { Value = "C-B" , Text = "Coptic - B" },
                    new SelectListItem { Value = "C-S",  Text = "Coptic - S" },
                    new SelectListItem { Value = "C-Sa", Text = "Coptic - Sa" },
                    new SelectListItem { Value = "C-Sf", Text = "Coptic - Sf" },
                    new SelectListItem { Value = "C-A",  Text = "Coptic - A" },
                    new SelectListItem { Value = "C-sA", Text = "Coptic - sA" },
                    new SelectListItem { Value = "C-F",  Text = "Coptic - F" },
                    new SelectListItem { Value = "C-Fb", Text = "Coptic - Fb" },
                    new SelectListItem { Value = "C-O",  Text = "Coptic - O" },
                    new SelectListItem { Value = "C-NH", Text = "Coptic - NH" }
                };
        }
        public MeaningsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Meanings
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Meanings.Include(m => m.ParentMeaning);
            return View(await applicationDbContext.ToListAsync());
        }





        // GET: Meanings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meaning = await _context.Meanings
                .Include(m => m.ParentMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (meaning == null)
            {
                return NotFound();
            }

            return View(meaning);
        }

        // GET: Meanings/Create
        public IActionResult Create()
        {
            ViewData["ParentMeaningID"] = new SelectList(_context.Meanings, "ID", "ID");
            return View();
        }

        // POST: Meanings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,MeaningText,Notes,Language,ParentMeaningID")] Meaning meaning)
        {
            if (ModelState.IsValid)
            {
                _context.Add(meaning);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentMeaningID"] = new SelectList(_context.Meanings, "ID", "ID", meaning.ParentMeaningID);
            return View(meaning);
        }

        // GET: Meanings/Edit/5
        // GET: Meanings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meaning = await _context.Meanings.FindAsync(id);
            if (meaning == null)
            {
                return NotFound();
            }
            ViewData["ParentMeaningID"] = new SelectList(_context.Meanings, "ID", "ID", meaning.ParentMeaningID);
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(meaning);
        }

        // POST: Meanings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,MeaningText,Notes,Language,ParentMeaningID")] Meaning meaning)
        {
            if (id != meaning.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(meaning);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeaningExists(meaning.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                var returnUrl = TempData["ReturnUrl"] as string;
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }
            ViewData["ParentMeaningID"] = new SelectList(_context.Meanings, "ID", "ID", meaning.ParentMeaningID);

            return View(meaning);
        }

        // GET: Meanings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meaning = await _context.Meanings
                .Include(m => m.ParentMeaning)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (meaning == null)
            {
                return NotFound();
            }

            return View(meaning);
        }

        // POST: Meanings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meaning = await _context.Meanings.FindAsync(id);
            if (meaning != null)
            {
                _context.Meanings.Remove(meaning);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MeaningExists(int id)
        {
            return _context.Meanings.Any(e => e.ID == id);
        }
    }
}
