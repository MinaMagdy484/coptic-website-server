using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CopticDictionarynew1.Services;
using DEPI_Project1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CopticDictionarynew1.Controllers
{
    [Authorize(Roles = "Student,Instructor,Admin")]
    public class WordsController : Controller
    {
        private readonly ILogger<WordsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;

        // Inject the logger and DbContext into the constructor
        public WordsController(ILogger<WordsController> logger, ApplicationDbContext context, IGoogleDriveService googleDriveService)
        {
            _logger = logger;
            _context = context;
            _googleDriveService = googleDriveService;
        }

        // GET: Words
        //public async Task<IActionResult> Index()
        //{
        //    var applicationDbContext = _context.Words.Include(w => w.GroupWord).Include(w => w.Root).AsSplitQuery();
        //    return View(await applicationDbContext.ToListAsync());
        //}

        //////Working Search
        //public async Task<IActionResult> Index(string? search)
        //{
        //    if (string.IsNullOrEmpty(search))
        //    {
        //        // If search is null or empty, return an empty view or any other result you prefer.
        //        return View("Index", new List<Word>());
        //    }

        //    ViewBag.SearchText = search;
        //    IQueryable<Word> Students = _context.Words.AsQueryable();

        //    // Search for students whose names contain the search string.
        //    Students = Students.Where(prof => prof.Word_text.Contains(search));

        //    var StudentsList = await Students.ToListAsync();
        //    return View("Index", StudentsList);
        //}

        //Search contain Working 
        //public async Task<IActionResult> Index(string? search)
        //{
        //    if (string.IsNullOrEmpty(search))
        //    {
        //        return View("Index", new List<Word>());
        //    }

        //    // Normalize the search string
        //    search = NormalizeString(search);

        //    // Store the search text in ViewBag
        //    ViewBag.SearchText = search;

        //    // Query the Words table (without the normalization in the SQL query)
        //    var wordsQuery = _context.Words.AsQueryable();

        //    // Pull the data into memory (with ToListAsync) and then apply the normalization on the client side
        //    var wordsList = await wordsQuery.ToListAsync(); // Asynchronous database query

        //    // Apply the normalization on the client-side
        //    wordsList = wordsList.Where(w => NormalizeString(w.Word_text).Contains(search)).ToList();

        //    return View("Index", wordsList);
        //}

        public async Task<IActionResult> Index(string? search, string? searchType = "exact", string? filterType = "word", 
    string? completionFilter = "", string? review1Filter = "", string? review2Filter = "", 
    bool? showCopticOnly = false, int page = 1, int pageSize = 20)
{
    var wordsQuery = _context.Words
        .Include(w => w.GroupWord)
        .Include(w => w.Root)
        .Include(w => w.WordMeanings)
            .ThenInclude(wm => wm.Meaning)
        .AsQueryable();

    // Store search parameters in ViewBag
    ViewBag.SearchText = search;
    ViewBag.SearchType = searchType;
    ViewBag.FilterType = filterType;
    ViewBag.CompletionFilter = completionFilter;
    ViewBag.Review1Filter = review1Filter;
    ViewBag.Review2Filter = review2Filter;
    ViewBag.ShowCopticOnly = showCopticOnly;
    ViewBag.CurrentPage = page;
    ViewBag.PageSize = pageSize;

    // Apply Coptic language filter first if selected
    if (showCopticOnly == true)
    {
        wordsQuery = wordsQuery.Where(w => w.Language.StartsWith("C-"));
    }

    // Apply completion status filters
    if (!string.IsNullOrEmpty(completionFilter))
    {
        switch (completionFilter.ToLower())
        {
            case "completed":
                wordsQuery = wordsQuery.Where(w => w.ISCompleted == true);
                break;
            case "incomplete":
                wordsQuery = wordsQuery.Where(w => w.ISCompleted == false);
                break;
            case "unknown":
                wordsQuery = wordsQuery.Where(w => w.ISCompleted == null);
                break;
        }
    }

    // Apply Review1 status filters
    if (!string.IsNullOrEmpty(review1Filter))
    {
        switch (review1Filter.ToLower())
        {
            case "passed":
                wordsQuery = wordsQuery.Where(w => w.Review1 == true);
                break;
            case "failed":
                wordsQuery = wordsQuery.Where(w => w.Review1 == false);
                break;
            case "pending":
                wordsQuery = wordsQuery.Where(w => w.Review1 == null);
                break;
        }
    }

    // Apply Review2 status filters
    if (!string.IsNullOrEmpty(review2Filter))
    {
        switch (review2Filter.ToLower())
        {
            case "passed":
                wordsQuery = wordsQuery.Where(w => w.Review2 == true);
                break;
            case "failed":
                wordsQuery = wordsQuery.Where(w => w.Review2 == false);
                break;
            case "pending":
                wordsQuery = wordsQuery.Where(w => w.Review2 == null);
                break;
        }
    }

    // Apply text search filters only if search text is provided
    if (!string.IsNullOrEmpty(search))
    {
        // Handle ID search with "ND-" prefix
        if (search.StartsWith("ND-", StringComparison.OrdinalIgnoreCase))
        {
            var idString = search.Substring(3); // Remove "ND-" prefix
            if (int.TryParse(idString, out int wordId))
            {
                wordsQuery = wordsQuery.Where(w => w.WordId == wordId);
            }
            else
            {
                // Invalid ID format, return empty results
                return View("Index", new PagedResult<Word>
                {
                    Items = new List<Word>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 0
                });
            }
        }
        else
        {
            // Normalize the search string for text searches
            var normalizedSearch = NormalizeString(search);

            switch (filterType?.ToLower())
            {
                case "group":
                    wordsQuery = FilterByGroup(wordsQuery, normalizedSearch, searchType);
                    break;
                case "meaning":
                case "definition":
                    wordsQuery = FilterByMeaning(wordsQuery, normalizedSearch, searchType);
                    break;
                case "word":
                default:
                    wordsQuery = FilterByWord(wordsQuery, normalizedSearch, searchType);
                    break;
            }
        }
    }

    // Get total count for pagination
    var totalCount = await wordsQuery.CountAsync();
    var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

    // Apply pagination
    var words = await wordsQuery
        .OrderBy(w => w.WordId)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var pagedResult = new PagedResult<Word>
    {
        Items = words,
        TotalCount = totalCount,
        CurrentPage = page,
        PageSize = pageSize,
        TotalPages = totalPages
    };

    return View("Index", pagedResult);
}

// Update the existing filter methods to work with the query that might already be filtered
private IQueryable<Word> FilterByWord(IQueryable<Word> query, string search, string searchType)
{
    // Get filtered words first, then apply normalization filtering in memory for better performance
    var filteredWords = query.ToList();
    
    var matchedWords = searchType?.ToLower() switch
    {
        "exact" => filteredWords.Where(w => NormalizeString(w.Word_text) == search),
        "contain" => filteredWords.Where(w => NormalizeString(w.Word_text).Contains(search)),
        "start" => filteredWords.Where(w => NormalizeString(w.Word_text).StartsWith(search)),
        "end" => filteredWords.Where(w => NormalizeString(w.Word_text).EndsWith(search)),
        _ => filteredWords.Where(w => NormalizeString(w.Word_text).StartsWith(search))
    };

    var wordIds = matchedWords.Select(w => w.WordId).ToList();
    return _context.Words
        .Include(w => w.GroupWord)
        .Include(w => w.Root)
        .Include(w => w.WordMeanings)
            .ThenInclude(wm => wm.Meaning)
        .Where(w => wordIds.Contains(w.WordId));
}

private IQueryable<Word> FilterByGroup(IQueryable<Word> query, string search, string searchType)
{
    // Get the group IDs first that match the search
    var allGroups = _context.Groups.ToList();
    
    var filteredGroups = searchType?.ToLower() switch
    {
        "exact" => allGroups.Where(g => NormalizeString(g.Name) == search),
        "contain" => allGroups.Where(g => NormalizeString(g.Name).Contains(search)),
        "start" => allGroups.Where(g => NormalizeString(g.Name).StartsWith(search)),
        "end" => allGroups.Where(g => NormalizeString(g.Name).EndsWith(search)),
        _ => allGroups.Where(g => NormalizeString(g.Name).StartsWith(search))
    };

    var groupIds = filteredGroups.Select(g => g.ID).ToList();
    return query.Where(w => w.GroupID.HasValue && groupIds.Contains(w.GroupID.Value));
}

private IQueryable<Word> FilterByMeaning(IQueryable<Word> query, string search, string searchType)
{
    var allMeanings = _context.Meanings.ToList();
    
    var filteredMeanings = searchType?.ToLower() switch
    {
        "exact" => allMeanings.Where(m => NormalizeString(m.MeaningText) == search),
        "contain" => allMeanings.Where(m => NormalizeString(m.MeaningText).Contains(search)),
        "start" => allMeanings.Where(m => NormalizeString(m.MeaningText).StartsWith(search)),
        "end" => allMeanings.Where(m => NormalizeString(m.MeaningText).EndsWith(search)),
        _ => allMeanings.Where(m => NormalizeString(m.MeaningText).StartsWith(search))
    };

    var meaningIds = filteredMeanings.Select(m => m.ID).ToList();
    return query.Where(w => w.WordMeanings.Any(wm => meaningIds.Contains(wm.MeaningID)));
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateCompletionStatus(int wordId, string fieldName, string value)
{
    try
    {
        var word = await _context.Words.FindAsync(wordId);
        if (word == null)
        {
            return Json(new { success = false, message = "Word not found" });
        }

        // Check permissions
        bool canUpdate = false;
        
        switch (fieldName)
        {
            case "ISCompleted":
                // Instructors and Admins can update completion status
                canUpdate = User.IsInRole("Instructor") || User.IsInRole("Admin");
                break;
            case "Review1":
            case "Review2":
                // Only Admins can update review statuses
                canUpdate = User.IsInRole("Admin");
                break;
            default:
                return Json(new { success = false, message = "Invalid field name" });
        }

        if (!canUpdate)
        {
            return Json(new { success = false, message = "You don't have permission to update this field" });
        }

        // Parse the value
        bool? boolValue = null;
        if (!string.IsNullOrEmpty(value))
        {
            if (bool.TryParse(value, out bool parsed))
            {
                boolValue = parsed;
            }
        }

        // Update the appropriate field
        switch (fieldName)
        {
            case "ISCompleted":
                word.ISCompleted = boolValue;
                break;
            case "Review1":
                word.Review1 = boolValue;
                break;
            case "Review2":
                word.Review2 = boolValue;
                break;
        }

        // Update audit fields
        word.ModifiedAt = DateTime.UtcNow;
        word.ModifiedByUserId = User.Identity.Name;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Status updated successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating completion status for word {WordId}", wordId);
        return Json(new { success = false, message = "An error occurred while updating the status" });
    }
}
        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Convert to lowercase
            input = input.ToLowerInvariant();

            // Optional: Remove diacritics (accents)
            input = RemoveDiacritics(input);

            return input;
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Normalize to form D (decomposed characters), remove non-spacing marks
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }




        public IActionResult CreateMeaning(int wordId)
        {
            ViewBag.WordId = wordId; // Pass the WordId to the view
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        // POST: CreateMeaning (Handles form submission, saves meaning, and links it to the word)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMeaning(int wordId, [Bind("MeaningText,Notes,Language")] Meaning meaning)
        {
            if (ModelState.IsValid)
            {
                // First, add the meaning to the database
                _context.Meanings.Add(meaning);
                await _context.SaveChangesAsync();

                // Ensure the Meaning has been successfully saved and has a valid ID
                if (meaning.ID > 0)
                {
                    // Now, create the WordMeaning relationship and save it
                    WordMeaning wordMeaning = new WordMeaning
                    {
                        WordID = wordId,        // This is the correct Word ID
                        MeaningID = meaning.ID
                        // This is the newly created Meaning ID
                    };

                    _context.WordMeanings.Add(wordMeaning);
                    await _context.SaveChangesAsync();

                    // Redirect back to the word details page
                    return RedirectToAction("Details", new { id = wordId });
                }
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            ViewBag.WordId = wordId; // If form is invalid, pass WordId again
            return View(meaning);
        }



        //public IActionResult SelectFromMeaning(int wordId)
        //{
        //    // Pass the list of existing meanings to the view
        //    ViewBag.Meanings = new SelectList(_context.Meanings.ToList(), "ID", "MeaningText");
        //    ViewBag.WordId = wordId; // Pass the WordId to the view
        //    return View();
        //}




        //public IActionResult SelectFromMeaning(int wordId)
        //{
        //    // Retrieve meanings with related WordMeanings and Word
        //    var meanings = _context.Meanings
        //        .Include(m => m.WordMeanings) // Include related WordMeanings
        //        .ThenInclude(wm => wm.Word)   // Include related Word for each WordMeaning
        //        .ToList();

        //    // Create a list of SelectListItem with formatted text
        //    var selectListItems = meanings.Select(m => new SelectListItem
        //    {
        //        Value = m.ID.ToString(), // Use Meaning ID as the value
        //        Text = $"{m.MeaningText} - [{string.Join(", ", m.WordMeanings.Select(wm => wm.Word.Word_text))}]" // Format the text
        //    }).ToList();

        //    // Pass the formatted SelectList to the view
        //    ViewBag.Meanings = new SelectList(selectListItems, "Value", "Text");
        //    ViewBag.WordId = wordId; // Pass the WordId to the view
        //    return View();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SelectFromMeaning(int wordId, int meaningID)
        //{
        //    // Check if the word exists in the database before proceeding
        //    var wordExists = _context.Words.Any(w => w.WordId == wordId);
        //    if (!wordExists)
        //    {
        //        ModelState.AddModelError("", "The selected word does not exist.");
        //        ViewBag.Meanings = new SelectList(_context.Meanings.ToList(), "ID", "MeaningText");
        //        ViewBag.WordId = wordId;
        //        return View();
        //    }
        //    if (ModelState.IsValid)
        //    {
        //        // Create the WordMeaning relationship
        //        WordMeaning wordMeaning = new WordMeaning
        //        {
        //            WordID = wordId,  // Link the selected word
        //            MeaningID = meaningID // Link the selected meaning from the dropdown
        //        };
        //        _context.WordMeanings.Add(wordMeaning);
        //        await _context.SaveChangesAsync();
        //        // Redirect to the word details page
        //        return RedirectToAction("Details", new { id = wordId });
        //    }
        //    // If form is invalid, repopulate the meanings dropdown
        //    ViewBag.Meanings = new SelectList(_context.Meanings.ToList(), "ID", "MeaningText");
        //    ViewBag.WordId = wordId;
        //    return View();
        //}

        //public IActionResult SelectFromMeaning(int wordId)
        //{
        //    // Retrieve meanings with related WordMeanings and Word
        //    var meanings = _context.Meanings
        //        .Include(m => m.WordMeanings)
        //        .ThenInclude(wm => wm.Word)
        //        .ToList();

        //    // Create a list of SelectListItem with formatted text
        //    var selectListItems = meanings.Select(m => new SelectListItem
        //    {
        //        Value = m.ID.ToString(),
        //        Text = $"{m.MeaningText} ({m.Language}) - [{string.Join(", ", m.WordMeanings.Select(wm => $"{wm.Word.Word_text} ({wm.Word.Language})"))}]"
        //    }).ToList();

        //    ViewBag.Meanings = new SelectList(selectListItems, "Value", "Text");
        //    ViewBag.WordId = wordId;
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SelectFromMeaning(int wordId, int meaningID)
        //{
        //    // Check if the word exists
        //    var wordExists = await _context.Words.AnyAsync(w => w.WordId == wordId);
        //    if (!wordExists)
        //    {
        //        ModelState.AddModelError("", "The selected word does not exist.");
        //        return View();
        //    }

        //    // Check if this word-meaning relationship already exists
        //    var existingRelationship = await _context.WordMeanings
        //        .AnyAsync(wm => wm.WordID == wordId && wm.MeaningID == meaningID);

        //    if (existingRelationship)
        //    {
        //        // If relationship exists, just redirect back to Details
        //        return RedirectToAction("Details", new { id = wordId });
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        // Create new word-meaning relationship only if it doesn't exist
        //        WordMeaning wordMeaning = new WordMeaning
        //        {
        //            WordID = wordId,
        //            MeaningID = meaningID
        //        };

        //        _context.WordMeanings.Add(wordMeaning);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Details", new { id = wordId });
        //    }

        //    // If we get here, something went wrong - repopulate the meanings dropdown
        //    var meanings = await _context.Meanings
        //        .Include(m => m.WordMeanings)
        //        .ThenInclude(wm => wm.Word)
        //        .ToListAsync();

        //    var selectListItems = meanings.Select(m => new SelectListItem
        //    {
        //        Value = m.ID.ToString(),
        //        Text = $"{m.MeaningText} ({m.Language}) - [{string.Join(", ", m.WordMeanings.Select(wm => $"{wm.Word.Word_text} ({wm.Word.Language})"))}]"
        //    }).ToList();

        //    ViewBag.Meanings = new SelectList(selectListItems, "Value", "Text");
        //    ViewBag.WordId = wordId;
        //    return View();
        //}

        public async Task<IActionResult> SelectFromMeaning(int wordId, string search = "")
        {
            // Check if the word exists in the database before proceeding
            var wordExists = await _context.Words.AnyAsync(w => w.WordId == wordId);
            if (!wordExists)
            {
                return NotFound($"Word with ID {wordId} not found.");
            }

            // Get the word name for display
            var word = await _context.Words.FindAsync(wordId);
            ViewBag.WordId = wordId;
            ViewBag.WordText = word?.Word_text ?? "Unknown Word";
            ViewBag.SearchText = search;

            if (string.IsNullOrEmpty(search))
            {
                ViewBag.AvailableMeanings = new List<Meaning>();
                return View();
            }

            // Normalize the search string
            search = NormalizeString(search);

            // Get existing meaning IDs for this word to exclude them (optional)
            var existingMeaningIds = await _context.WordMeanings
                .Where(wm => wm.WordID == wordId)
                .Select(wm => wm.MeaningID)
                .ToListAsync();

            // Get all parent meanings with their associated words
            var allMeanings = await _context.Meanings
                .Where(m => m.ParentMeaningID == null) // Only parent meanings (where ParentMeaningID is null)
                .Include(m => m.WordMeanings)
                .ThenInclude(wm => wm.Word)
                .ToListAsync();

            // Filter meanings based on search criteria (meaning text OR words associated with the meaning)
            var filteredMeanings = allMeanings
                .Where(m =>
                    // Search in meaning text
                    NormalizeString(m.MeaningText ?? "").Contains(search) ||
                    // Search in words associated with this meaning
                    (m.WordMeanings != null && m.WordMeanings.Any(wm =>
                        wm.Word != null && NormalizeString(wm.Word.Word_text ?? "").Contains(search)))
                )
                .OrderBy(m => m.MeaningText)
                .ToList();

            ViewBag.AvailableMeanings = filteredMeanings;
            return View();
        }

        // POST method to handle the form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeaningToWord(int wordId, int meaningId)
        {
            // Check if relation already exists
            var existingRelation = await _context.WordMeanings
                .FirstOrDefaultAsync(wm => wm.WordID == wordId && wm.MeaningID == meaningId);

            if (existingRelation != null)
            {
                TempData["Warning"] = "This word is already linked to the selected meaning.";
                return RedirectToAction("Details", new { id = wordId });
            }

            var newWordMeaning = new WordMeaning
            {
                WordID = wordId,
                MeaningID = meaningId
            };

            _context.WordMeanings.Add(newWordMeaning);
            await _context.SaveChangesAsync();

            var meaning = await _context.Meanings.FindAsync(meaningId);
            var meaningText = meaning?.MeaningText ?? "";
            var truncatedText = meaningText.Length > 50 ? meaningText.Substring(0, 50) + "..." : meaningText;

            TempData["Message"] = $"Meaning '{truncatedText}' has been linked to the word successfully.";

            return RedirectToAction("Details", new { id = wordId });
        }







        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Words/DeleteConfirmedWordMeaning/{id}")]
        public async Task<IActionResult> DeleteConfirmedWordMeaning(int id)
        {
            var wordMeaning = await _context.WordMeanings.FindAsync(id);
            if (wordMeaning != null)
            {
                _context.WordMeanings.Remove(wordMeaning);
                await _context.SaveChangesAsync();
                return Json(new { success = true });  // Return JSON indicating success
            }

            return Json(new { success = false, message = "Word meaning not found." });
        }


        // GET: WordMeanings/Delete/5
        public async Task<IActionResult> DeleteWM(int? id)
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
        //21/6/2025
        // POST: WordMeanings/DeleteConfirmed
        // [HttpPost, ActionName("DeleteWM")]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> DeleteConfirmedWM(int id)
        // {
        //     var wordMeaning = await _context.WordMeanings.FindAsync(id);
        //     if (wordMeaning != null)
        //     {
        //         _context.WordMeanings.Remove(wordMeaning);
        //         await _context.SaveChangesAsync();
        //     }

        //     return RedirectToAction(nameof(Index));
        // }
        // ...existing code...

        [HttpPost, ActionName("DeleteWM")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedWM(int id, int? wordId = null)
        {
            var wordMeaning = await _context.WordMeanings.FindAsync(id);
            if (wordMeaning != null)
            {
                _context.WordMeanings.Remove(wordMeaning);
                await _context.SaveChangesAsync();
            }

            // Redirect back to the word details page if wordId is provided
            if (wordId.HasValue)
            {
                return RedirectToAction("Details", new { id = wordId });
            }

            return RedirectToAction(nameof(Index));
        }

        // ...existing code...



        //.Include(w => w.WordMeanings)
        //    .ThenInclude(wm => wm.Examples) // Include examples for word meanings
        //.Include(w => w.WordMeanings)
        //    .ThenInclude(wm => wm.WordMeaningBibles) // Include Bible references for word meanings
        //        .ThenInclude(wmb => wmb.Bible) // Include Bible details

        //   9/7/2025
        // public async Task<IActionResult> Details(int? id)
        // {
        //     if (id == null)
        //     {
        //         return NotFound();
        //     }

        //     var word = await _context.Words
        //         .Include(w => w.CreatedByUser)        // Include audit info
        //         .Include(w => w.ModifiedByUser)       // Include audit info
        //         .Include(w => w.WordExplanations)
        //         .Include(w => w.GroupWord)
        //             .ThenInclude(g => g.Words)
        //         .Include(w => w.GroupWord)
        //             .ThenInclude(g => g.GroupExplanations)
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Meaning)
        //                 .ThenInclude(m => m.CreatedByUser)        // Include audit info for meanings
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Meaning)
        //                 .ThenInclude(m => m.ModifiedByUser)       // Include audit info for meanings
        //                 .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.Meaning)
        //         .ThenInclude(m => m.ChildMeanings)
        //             .ThenInclude(cm => cm.CreatedByUser)  // Include audit info for child meanings
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Meaning)
        //                 .ThenInclude(m => m.ChildMeanings)
        //                     .ThenInclude(cm => cm.ModifiedByUser) // Include audit info for child meanings
        //                             .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.Examples)
        //         .ThenInclude(e => e.CreatedByUser)         // Include audit info for examples
        // .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.Examples)
        //         .ThenInclude(e => e.ModifiedByUser)        // Include audit info for examples
        // .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.Examples)
        //         .ThenInclude(e => e.ChildExamples)
        //             .ThenInclude(ce => ce.CreatedByUser)   // Include audit info for child examples
        // .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.Examples)
        //         .ThenInclude(e => e.ChildExamples)
        //             .ThenInclude(ce => ce.ModifiedByUser)  // Include audit info for child examples
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Meaning)
        //             .ThenInclude(m => m.ChildMeanings)
        //         .Include(w => w.WordMeanings) // Include current word's meanings
        //             .ThenInclude(wm => wm.WordMeaningBibles) // Include Bible references directly
        //             .ThenInclude(wmb => wmb.Bible) // Include Bible details
        //         .Include(w => w.DictionaryReferenceWords)
        //             .ThenInclude(drw => drw.Dictionary)
        //         .Include(w => w.Root)
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Meaning)
        //             .ThenInclude(m => m.WordMeanings)
        //             .ThenInclude(wm => wm.Word)
        //                     .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.WordMeaningBibles)
        //         .ThenInclude(wmb => wmb.CreatedByUser)      // Include audit info for WordMeaningBible
        // .Include(w => w.WordMeanings)
        //     .ThenInclude(wm => wm.WordMeaningBibles)
        //         .ThenInclude(wmb => wmb.ModifiedByUser)     // Include audit info for WordMeaningBible
        //         .Include(w => w.WordMeanings)
        //             .ThenInclude(wm => wm.Examples)
        //             .ThenInclude(e => e.ChildExamples)
        //         .Include(w => w.WordExplanations)           // Include WordExplanations
        //             .ThenInclude(we => we.CreatedByUser)    // Include audit info for explanations
        //         .Include(w => w.WordExplanations)
        //             .ThenInclude(we => we.ModifiedByUser)   // Include audit info for explanations
        //         .AsSplitQuery()
        //         .FirstOrDefaultAsync(m => m.WordId == id);

        //     if (word == null)
        //     {
        //         return NotFound();
        //     }

        //     // For each WordMeaning of the current word only
        //     foreach (var wordMeaning in word.WordMeanings)
        //     {
        //         foreach (var wordMeaningBible in wordMeaning.WordMeaningBibles)
        //         {
        //             var bible = wordMeaningBible.Bible;

        //             // Fetch other Bible verses with the same Book, Chapter, and Verse but different Language or Edition
        //             var relatedBibleVerses = await _context.Bibles
        //                 .Where(b => b.Book == bible.Book &&
        //                            b.Chapter == bible.Chapter &&
        //                            b.Verse == bible.Verse &&
        //                            (b.Language != bible.Language || b.Edition != bible.Edition))
        //                 .ToListAsync();

        //             // Store these related Bible verses in the ViewBag
        //             ViewBag.RelatedBibleVerses ??= new Dictionary<int, List<Bible>>();
        //             ViewBag.RelatedBibleVerses[wordMeaningBible.BibleID] = relatedBibleVerses;
        //         }
        //     }


        //     if (word?.GroupWord?.Words != null)
        //     {
        //         // Group words by language and class
        //         var groupedWords = word.GroupWord.Words
        //             .GroupBy(w => w.Language)
        //             .OrderBy(g => g.Key)
        //             .ToDictionary(
        //                 g => g.Key,
        //                 g => g.GroupBy(w => w.Class)
        //                       .OrderBy(c => c.Key)
        //                       .ToDictionary(c => c.Key, c => c.ToList())
        //             );

        //         // Get unique classes across all words
        //         var uniqueClasses = word.GroupWord.Words
        //             .Select(w => w.Class)
        //             .Distinct()
        //             .OrderBy(c => c)
        //             .ToList();

        //         ViewBag.GroupedWords = groupedWords;
        //         ViewBag.UniqueClasses = uniqueClasses;
        //     }

        //     return View(word);
        // }


        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var word = await _context.Words
        //        .Include(w => w.CreatedByUser)        // Include audit info
        //        .Include(w => w.ModifiedByUser)       // Include audit info
        //        .Include(w => w.WordExplanations)
        //            .ThenInclude(we => we.CreatedByUser)    // Include audit info for explanations
        //        .Include(w => w.WordExplanations)
        //            .ThenInclude(we => we.ModifiedByUser)   // Include audit info for explanations
        //        .Include(w => w.GroupWord)
        //            .ThenInclude(g => g.Words)
        //        .Include(w => w.GroupWord)
        //            .ThenInclude(g => g.GroupExplanations)
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.CreatedByUser)        // Include audit info for meanings
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.ModifiedByUser)       // Include audit info for meanings
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.ChildMeanings)
        //                    .ThenInclude(cm => cm.CreatedByUser)  // Include audit info for child meanings
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.ChildMeanings)
        //                    .ThenInclude(cm => cm.ModifiedByUser) // Include audit info for child meanings
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Examples)
        //                .ThenInclude(e => e.CreatedByUser)         // Include audit info for examples
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Examples)
        //                .ThenInclude(e => e.ModifiedByUser)        // Include audit info for examples
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Examples)
        //                .ThenInclude(e => e.ChildExamples)
        //                    .ThenInclude(ce => ce.CreatedByUser)   // Include audit info for child examples
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Examples)
        //                .ThenInclude(e => e.ChildExamples)
        //                    .ThenInclude(ce => ce.ModifiedByUser)  // Include audit info for child examples
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.ChildMeanings)
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.WordMeaningBibles)
        //                .ThenInclude(wmb => wmb.CreatedByUser)      // Include audit info for WordMeaningBible
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.WordMeaningBibles)
        //                .ThenInclude(wmb => wmb.ModifiedByUser)     // Include audit info for WordMeaningBible
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.WordMeaningBibles)
        //                .ThenInclude(wmb => wmb.Bible)
        //        .Include(w => w.DictionaryReferenceWords)
        //            .ThenInclude(drw => drw.Dictionary)
        //        .Include(w => w.Root)
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning)
        //                .ThenInclude(m => m.WordMeanings)
        //                    .ThenInclude(wm => wm.Word)
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Examples)
        //                .ThenInclude(e => e.ChildExamples)
        //        .AsSplitQuery()
        //        .FirstOrDefaultAsync(m => m.WordId == id);

        //    if (word == null)
        //    {
        //        return NotFound();
        //    }

        //    // For each WordMeaning of the current word only
        //    foreach (var wordMeaning in word.WordMeanings ?? Enumerable.Empty<WordMeaning>())
        //    {
        //        foreach (var wordMeaningBible in wordMeaning.WordMeaningBibles ?? Enumerable.Empty<WordMeaningBible>())
        //        {
        //            var bible = wordMeaningBible.Bible;
        //            if (bible != null)
        //            {
        //                // Fetch other Bible verses with the same Book, Chapter, and Verse but different Language or Edition
        //                var relatedBibleVerses = await _context.Bibles
        //                    .Where(b => b.Book == bible.Book &&
        //                               b.Chapter == bible.Chapter &&
        //                               b.Verse == bible.Verse &&
        //                               (b.Language != bible.Language || b.Edition != bible.Edition))
        //                    .ToListAsync();

        //                // Store these related Bible verses in the ViewBag
        //                ViewBag.RelatedBibleVerses ??= new Dictionary<int, List<Bible>>();
        //                ViewBag.RelatedBibleVerses[wordMeaningBible.BibleID] = relatedBibleVerses;
        //            }
        //        }
        //    }

        //    // Handle grouping with null safety
        //    if (word?.GroupWord?.Words != null)
        //    {
        //        // Group words by language and class - Handle null values properly
        //        var groupedWords = word.GroupWord.Words
        //            .GroupBy(w => w.Language ?? "Unknown") // Handle null Language
        //            .OrderBy(g => g.Key)
        //            .ToDictionary(
        //                g => g.Key,
        //                g => g.GroupBy(w => w.Class ?? "Unknown") // Handle null Class
        //                      .OrderBy(c => c.Key)
        //                      .ToDictionary(c => c.Key, c => c.ToList())
        //            );

        //        // Get unique classes across all words - Handle null values
        //        var uniqueClasses = word.GroupWord.Words
        //            .Select(w => w.Class ?? "Unknown") // Handle null Class
        //            .Distinct()
        //            .OrderBy(c => c)
        //            .ToList();

        //        ViewBag.GroupedWords = groupedWords;
        //        ViewBag.UniqueClasses = uniqueClasses;
        //    }
        //    else
        //    {
        //        // Initialize empty collections if GroupWord or Words is null
        //        ViewBag.GroupedWords = new Dictionary<string, Dictionary<string, List<Word>>>();
        //        ViewBag.UniqueClasses = new List<string>();
        //    }

        //    return View(word);
        //}


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Load basic word information first (fastest query)
            var word = await _context.Words
                .AsNoTracking()
                .Where(w => w.WordId == id)
                .Select(w => new Word
                {
                    WordId = w.WordId,
                    Word_text = w.Word_text ?? "",
                    Language = w.Language ?? "",
                    Class = w.Class ?? "",
                    notes = w.notes ?? "",
                    IPA = w.IPA ?? "",
                    Pronunciation = w.Pronunciation ?? "",
                    RootID = w.RootID,
                    GroupID = w.GroupID,
                    ISCompleted = w.ISCompleted,
                    Review1 = w.Review1,
                    Review2 = w.Review2,
                    CreatedAt = w.CreatedAt,
                    ModifiedAt = w.ModifiedAt,
                    CreatedByUserId = w.CreatedByUserId ?? "",
                    ModifiedByUserId = w.ModifiedByUserId ?? ""
                })
                .FirstOrDefaultAsync();

            if (word == null)
            {
                return NotFound();
            }

            // 2. Load Root word if exists
            if (word.RootID.HasValue)
            {
                word.Root = await _context.Words
                    .AsNoTracking()
                    .Where(w => w.WordId == word.RootID)
                    .Select(w => new Word
                    {
                        WordId = w.WordId,
                        Word_text = w.Word_text ?? "",
                        Class = w.Class ?? ""
                    })
                    .FirstOrDefaultAsync();
            }

            // 3. Load Group information if exists
            if (word.GroupID.HasValue)
            {
                word.GroupWord = await _context.Groups
                    .AsNoTracking()
                    .Where(g => g.ID == word.GroupID)
                    .Select(g => new GroupWord
                    {
                        ID = g.ID,
                        Name = g.Name ?? "",
                        Etymology = g.Etymology ?? "",
                        Words = g.Words.Select(gw => new Word
                        {
                            WordId = gw.WordId,
                            Word_text = gw.Word_text ?? "",
                            Language = gw.Language ?? "",
                            Class = gw.Class ?? ""
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                // Setup ViewBag for grouped words display
                if (word.GroupWord?.Words != null && word.GroupWord.Words.Any())
                {
                    var groupedWords = word.GroupWord.Words
                        .GroupBy(w => w.Language ?? "Unknown")
                        .OrderBy(g => g.Key)
                        .ToDictionary(
                            g => g.Key,
                            g => g.GroupBy(w => w.Class ?? "Unknown")
                                  .OrderBy(c => c.Key)
                                  .ToDictionary(c => c.Key, c => c.ToList())
                        );

                    var uniqueClasses = word.GroupWord.Words
                        .Select(w => w.Class ?? "Unknown")
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    ViewBag.GroupedWords = groupedWords;
                    ViewBag.UniqueClasses = uniqueClasses;
                }
                else
                {
                    ViewBag.GroupedWords = new Dictionary<string, Dictionary<string, List<Word>>>();
                    ViewBag.UniqueClasses = new List<string>();
                }
            }

            // 4. Load Word Explanations
            word.WordExplanations = await _context.WordExplanations
                .AsNoTracking()
                .Where(we => we.WordID == word.WordId)
                .Select(we => new WordExplanation
                {
                    ID = we.ID,
                    Explanation = we.Explanation ?? "",
                    Language = we.Language ?? "",
                    Notes = we.Notes ?? "",
                    CreatedAt = we.CreatedAt,
                    ModifiedAt = we.ModifiedAt,
                    CreatedByUserId = we.CreatedByUserId ?? "",
                    ModifiedByUserId = we.ModifiedByUserId ?? ""
                })
                .ToListAsync();

            // 5. Load Dictionary References
            word.DictionaryReferenceWords = await _context.DictionaryReferenceWords
                .AsNoTracking()
                .Where(drw => drw.WordID == word.WordId)
                .Select(drw => new DictionaryReferenceWord
                {
                    ID = drw.ID,
                    Reference = drw.Reference,
                    Column = drw.Column,
                    Dictionary = new Dictionary
                    {
                        DictionaryName = drw.Dictionary.DictionaryName ?? "",
                        Abbreviation = drw.Dictionary.Abbreviation ?? "",
                        Detils = drw.Dictionary.Detils ?? "",
                        MaxNumberOfPages = drw.Dictionary.MaxNumberOfPages ?? 0
                    }
                })
                .ToListAsync();

            // 6. Load Word Meanings with related data (most complex part)
            word.WordMeanings = await LoadWordMeaningsOptimized(word.WordId);

            // 7. Load user information for audit trails
            await LoadUserInformation(word);

            // 8. Load Bible verse relationships
            await LoadBibleReferences(word);

            return View(word);
        }

        // Separate method to load word meanings efficiently
        private async Task<List<WordMeaning>> LoadWordMeaningsOptimized(int wordId)
        {
            // First, get the basic word meanings
            var wordMeanings = await _context.WordMeanings
                .AsNoTracking()
                .Where(wm => wm.WordID == wordId)
                .Select(wm => new WordMeaning
                {
                    ID = wm.ID,
                    WordID = wm.WordID,
                    MeaningID = wm.MeaningID,
                    Word = new Word
                    {
                        WordId = wm.Word.WordId,
                        Word_text = wm.Word.Word_text ?? "",
                        Language = wm.Word.Language ?? "",
                        Class = wm.Word.Class ?? ""
                    },
                    Meaning = new Meaning
                    {
                        ID = wm.Meaning.ID,
                        MeaningText = wm.Meaning.MeaningText ?? "",
                        Language = wm.Meaning.Language ?? "",
                        Notes = wm.Meaning.Notes ?? "",
                        CreatedAt = wm.Meaning.CreatedAt,
                        ModifiedAt = wm.Meaning.ModifiedAt,
                        CreatedByUserId = wm.Meaning.CreatedByUserId ?? "",
                        ModifiedByUserId = wm.Meaning.ModifiedByUserId ?? ""
                    }
                })
                .ToListAsync();

            var meaningIds = wordMeanings.Select(wm => wm.MeaningID).ToList();

            if (meaningIds.Any())
            {
                // Load child meanings
                var childMeanings = await _context.Meanings
                    .AsNoTracking()
                    .Where(m => meaningIds.Contains(m.ParentMeaningID.Value))
                    .Select(m => new Meaning
                    {
                        ID = m.ID,
                        ParentMeaningID = m.ParentMeaningID,
                        MeaningText = m.MeaningText ?? "",
                        Language = m.Language ?? "",
                        Notes = m.Notes ?? "",
                        CreatedAt = m.CreatedAt,
                        ModifiedAt = m.ModifiedAt,
                        CreatedByUserId = m.CreatedByUserId ?? "",
                        ModifiedByUserId = m.ModifiedByUserId ?? ""
                    })
                    .ToListAsync();

                // Load related words for each meaning (excluding current word)
                var relatedWordMeanings = await _context.WordMeanings
                    .AsNoTracking()
                    .Where(wm => meaningIds.Contains(wm.MeaningID) && wm.WordID != wordId)
                    .Select(wm => new WordMeaning
                    {
                        ID = wm.ID,
                        WordID = wm.WordID,
                        MeaningID = wm.MeaningID,
                        Word = new Word
                        {
                            WordId = wm.Word.WordId,
                            Word_text = wm.Word.Word_text ?? "",
                            Language = wm.Word.Language ?? "",
                            Class = wm.Word.Class ?? ""
                        }
                    })
                    .ToListAsync();

                // Load examples
                var examples = await _context.Examples
                    .AsNoTracking()
                    .Where(e => e.WordMeaning.WordID == wordId)
                    .Select(e => new Example
                    {
                        ID = e.ID,
                        WordMeaningID = e.WordMeaningID,
                        ExampleText = e.ExampleText ?? "",
                        Reference = e.Reference ?? "",
                        Pronunciation = e.Pronunciation ?? "",
                        Notes = e.Notes ?? "",
                        Language = e.Language ?? "",
                        ParentExampleID = e.ParentExampleID,
                        CreatedAt = e.CreatedAt,
                        ModifiedAt = e.ModifiedAt,
                        CreatedByUserId = e.CreatedByUserId ?? "",
                        ModifiedByUserId = e.ModifiedByUserId ?? ""
                    })
                    .ToListAsync();

                // Load Bible references
                var bibleReferences = await _context.WordMeaningBibles
                    .AsNoTracking()
                    .Where(wmb => meaningIds.Contains(wmb.WordMeaning.MeaningID))
                    .Select(wmb => new WordMeaningBible
                    {
                        ID = wmb.ID,
                        WordMeaningID = wmb.WordMeaningID,
                        BibleID = wmb.BibleID,
                        CreatedAt = wmb.CreatedAt,
                        ModifiedAt = wmb.ModifiedAt,
                        CreatedByUserId = wmb.CreatedByUserId ?? "",
                        ModifiedByUserId = wmb.ModifiedByUserId ?? "",
                        Bible = new Bible
                        {
                            BibleID = wmb.Bible.BibleID,
                            Book = wmb.Bible.Book,
                            Chapter = wmb.Bible.Chapter,
                            Verse = wmb.Bible.Verse,
                            Text = wmb.Bible.Text ?? "",
                            Language = wmb.Bible.Language ?? "",
                            Edition = wmb.Bible.Edition ?? ""
                        }
                    })
                    .ToListAsync();

                // Assemble the complete structure
                foreach (var wm in wordMeanings)
                {
                    // Assign child meanings
                    wm.Meaning.ChildMeanings = childMeanings
                        .Where(cm => cm.ParentMeaningID == wm.MeaningID)
                        .ToList();

                    // Assign related words
                    wm.Meaning.WordMeanings = relatedWordMeanings
                        .Where(rwm => rwm.MeaningID == wm.MeaningID)
                        .ToList();

                    // Assign examples and child examples
                    var parentExamples = examples.Where(e => e.WordMeaningID == wm.ID && !e.ParentExampleID.HasValue).ToList();
                    foreach (var example in parentExamples)
                    {
                        example.ChildExamples = examples.Where(e => e.ParentExampleID == example.ID).ToList();
                    }
                    wm.Examples = parentExamples;

                    // Assign Bible references
                    wm.WordMeaningBibles = bibleReferences.Where(br => br.WordMeaningID == wm.ID).ToList();
                }
            }

            return wordMeanings;
        }

        // Load user information efficiently
        private async Task LoadUserInformation(Word word)
        {
            var userIds = new HashSet<string>();

            // Collect all user IDs
            if (!string.IsNullOrEmpty(word.CreatedByUserId)) userIds.Add(word.CreatedByUserId);
            if (!string.IsNullOrEmpty(word.ModifiedByUserId)) userIds.Add(word.ModifiedByUserId);

            foreach (var explanation in word.WordExplanations ?? new List<WordExplanation>())
            {
                if (!string.IsNullOrEmpty(explanation.CreatedByUserId)) userIds.Add(explanation.CreatedByUserId);
                if (!string.IsNullOrEmpty(explanation.ModifiedByUserId)) userIds.Add(explanation.ModifiedByUserId);
            }

            foreach (var wm in word.WordMeanings ?? new List<WordMeaning>())
            {
                if (!string.IsNullOrEmpty(wm.Meaning?.CreatedByUserId)) userIds.Add(wm.Meaning.CreatedByUserId);
                if (!string.IsNullOrEmpty(wm.Meaning?.ModifiedByUserId)) userIds.Add(wm.Meaning.ModifiedByUserId);

                foreach (var cm in wm.Meaning?.ChildMeanings ?? new List<Meaning>())
                {
                    if (!string.IsNullOrEmpty(cm.CreatedByUserId)) userIds.Add(cm.CreatedByUserId);
                    if (!string.IsNullOrEmpty(cm.ModifiedByUserId)) userIds.Add(cm.ModifiedByUserId);
                }

                foreach (var ex in wm.Examples ?? new List<Example>())
                {
                    if (!string.IsNullOrEmpty(ex.CreatedByUserId)) userIds.Add(ex.CreatedByUserId);
                    if (!string.IsNullOrEmpty(ex.ModifiedByUserId)) userIds.Add(ex.ModifiedByUserId);

                    foreach (var ce in ex.ChildExamples ?? new List<Example>())
                    {
                        if (!string.IsNullOrEmpty(ce.CreatedByUserId)) userIds.Add(ce.CreatedByUserId);
                        if (!string.IsNullOrEmpty(ce.ModifiedByUserId)) userIds.Add(ce.ModifiedByUserId);
                    }
                }

                foreach (var wmb in wm.WordMeaningBibles ?? new List<WordMeaningBible>())
                {
                    if (!string.IsNullOrEmpty(wmb.CreatedByUserId)) userIds.Add(wmb.CreatedByUserId);
                    if (!string.IsNullOrEmpty(wmb.ModifiedByUserId)) userIds.Add(wmb.ModifiedByUserId);
                }
            }

            // Load users in one query
            if (userIds.Any())
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u);

                // Assign users to entities
                AssignUsersToEntities(word, users);
            }
        }

        // Helper method to assign users to entities
        private void AssignUsersToEntities(Word word, Dictionary<string, ApplicationUser> users)
        {
            // Assign to main word
            if (!string.IsNullOrEmpty(word.CreatedByUserId) && users.ContainsKey(word.CreatedByUserId))
                word.CreatedByUser = users[word.CreatedByUserId];
            if (!string.IsNullOrEmpty(word.ModifiedByUserId) && users.ContainsKey(word.ModifiedByUserId))
                word.ModifiedByUser = users[word.ModifiedByUserId];

            // Assign to explanations
            foreach (var explanation in word.WordExplanations ?? new List<WordExplanation>())
            {
                if (!string.IsNullOrEmpty(explanation.CreatedByUserId) && users.ContainsKey(explanation.CreatedByUserId))
                    explanation.CreatedByUser = users[explanation.CreatedByUserId];
                if (!string.IsNullOrEmpty(explanation.ModifiedByUserId) && users.ContainsKey(explanation.ModifiedByUserId))
                    explanation.ModifiedByUser = users[explanation.ModifiedByUserId];
            }

            // Assign to meanings and related entities
            foreach (var wm in word.WordMeanings ?? new List<WordMeaning>())
            {
                if (wm.Meaning != null)
                {
                    if (!string.IsNullOrEmpty(wm.Meaning.CreatedByUserId) && users.ContainsKey(wm.Meaning.CreatedByUserId))
                        wm.Meaning.CreatedByUser = users[wm.Meaning.CreatedByUserId];
                    if (!string.IsNullOrEmpty(wm.Meaning.ModifiedByUserId) && users.ContainsKey(wm.Meaning.ModifiedByUserId))
                        wm.Meaning.ModifiedByUser = users[wm.Meaning.ModifiedByUserId];

                    // Child meanings
                    foreach (var cm in wm.Meaning.ChildMeanings ?? new List<Meaning>())
                    {
                        if (!string.IsNullOrEmpty(cm.CreatedByUserId) && users.ContainsKey(cm.CreatedByUserId))
                            cm.CreatedByUser = users[cm.CreatedByUserId];
                        if (!string.IsNullOrEmpty(cm.ModifiedByUserId) && users.ContainsKey(cm.ModifiedByUserId))
                            cm.ModifiedByUser = users[cm.ModifiedByUserId];
                    }
                }

                // Examples
                foreach (var ex in wm.Examples ?? new List<Example>())
                {
                    if (!string.IsNullOrEmpty(ex.CreatedByUserId) && users.ContainsKey(ex.CreatedByUserId))
                        ex.CreatedByUser = users[ex.CreatedByUserId];
                    if (!string.IsNullOrEmpty(ex.ModifiedByUserId) && users.ContainsKey(ex.ModifiedByUserId))
                        ex.ModifiedByUser = users[ex.ModifiedByUserId];

                    foreach (var ce in ex.ChildExamples ?? new List<Example>())
                    {
                        if (!string.IsNullOrEmpty(ce.CreatedByUserId) && users.ContainsKey(ce.CreatedByUserId))
                            ce.CreatedByUser = users[ce.CreatedByUserId];
                        if (!string.IsNullOrEmpty(ce.ModifiedByUserId) && users.ContainsKey(ce.ModifiedByUserId))
                            ce.ModifiedByUser = users[ce.ModifiedByUserId];
                    }
                }

                // Bible references
                foreach (var wmb in wm.WordMeaningBibles ?? new List<WordMeaningBible>())
                {
                    if (!string.IsNullOrEmpty(wmb.CreatedByUserId) && users.ContainsKey(wmb.CreatedByUserId))
                        wmb.CreatedByUser = users[wmb.CreatedByUserId];
                    if (!string.IsNullOrEmpty(wmb.ModifiedByUserId) && users.ContainsKey(wmb.ModifiedByUserId))
                        wmb.ModifiedByUser = users[wmb.ModifiedByUserId];
                }
            }
        }

        // Load related Bible verses efficiently
        private async Task LoadBibleReferences(Word word)
        {
            var bibleIds = word.WordMeanings?
                .SelectMany(wm => wm.WordMeaningBibles ?? new List<WordMeaningBible>())
                .Select(wmb => wmb.BibleID)
                .Distinct()
                .ToList() ?? new List<int>();

            ViewBag.RelatedBibleVerses = new Dictionary<int, List<Bible>>();

            if (bibleIds.Any())
            {
                try
                {
                    // Get unique Bible verse references
                    var bibleVerses = await _context.Bibles
                        .AsNoTracking()
                        .Where(b => bibleIds.Contains(b.BibleID))
                        .Select(b => new { b.BibleID, b.Book, b.Chapter, b.Verse, b.Language, b.Edition })
                        .ToListAsync();

                    // Group by verse reference to find related versions
                    var verseGroups = bibleVerses.GroupBy(b => new { b.Book, b.Chapter, b.Verse });

                    foreach (var verseGroup in verseGroups)
                    {
                        var relatedVerses = await _context.Bibles
                            .AsNoTracking()
                            .Where(b => b.Book == verseGroup.Key.Book &&
                                       b.Chapter == verseGroup.Key.Chapter &&
                                       b.Verse == verseGroup.Key.Verse)
                            .Where(b => !bibleIds.Contains(b.BibleID)) // Exclude the main references
                            .Select(b => new Bible
                            {
                                BibleID = b.BibleID,
                                Book = b.Book,
                                Chapter = b.Chapter,
                                Verse = b.Verse,
                                Text = b.Text ?? "",
                                Language = b.Language ?? "",
                                Edition = b.Edition ?? ""
                            })
                            .ToListAsync();

                        // Assign to each main reference
                        foreach (var mainVerse in verseGroup)
                        {
                            ViewBag.RelatedBibleVerses[mainVerse.BibleID] = relatedVerses;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading Bible verses for word {WordId}", word.WordId);
                    ViewBag.RelatedBibleVerses = new Dictionary<int, List<Bible>>();
                }
            }
        }











        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var word = await _context.Words
        //        .Include(w => w.GroupWord)
        //            .ThenInclude(g => g.Words) // Include words in the GroupWord
        //        .Include(w => w.GroupWord)
        //            .ThenInclude(g => g.GroupExplanations) // Include group explanations
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.Meaning) // Include the meaning for the word
        //                                           //.Include(w => w.WordMeanings)
        //                                           //    .ThenInclude(wm => wm.Examples) // Include examples for word meanings
        //        .Include(w => w.WordMeanings) // Include the current word's meanings
        //            .ThenInclude(wm => wm.Meaning) // Include each meaning
        //            .ThenInclude(m => m.WordMeanings) // Include all words linked to each meaning
        //            .ThenInclude(wm => wm.Examples)
        //        .Include(w => w.WordMeanings)
        //            .ThenInclude(wm => wm.WordMeaningBibles) // Include Bible references for word meanings
        //                .ThenInclude(wmb => wmb.Bible) // Include Bible details
        //        .Include(w => w.DictionaryReferenceWords)
        //            .ThenInclude(drw => drw.Dictionary) // Include dictionary references
        //        .Include(w => w.DrevWords)
        //            .ThenInclude(dw => dw.Word2)
        //    .Include(w => w.Root) // Include the root word
        //    .Include(w => w.WordMeanings)
        //        .ThenInclude(wm => wm.Meaning) // Navigate to the meaning
        //            .ThenInclude(m => m.WordMeanings) // Include related WordMeanings
        //            .ThenInclude(wm => wm.Word) // Include the Word related to the Meaning
        //            .AsSplitQuery()
        //    .FirstOrDefaultAsync(m => m.WordId == id); // Fetch the word by its ID
        //    if (word == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(word);
        //}
        //    var word = await _context.Words
        //    .Include(w => w.WordMeanings) // Include the current word's meanings
        //       .ThenInclude(wm => wm.Meaning) // Include each meaning
        //         .ThenInclude(m => m.WordMeanings) // Include all words linked to each meaning
        //         .ThenInclude(wm => wm.Examples) // Include examples for each word meaning
        //    .AsSplitQuery() // Prevent N+1 queries
        //.FirstOrDefaultAsync(m => m.WordId == id);


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

        private string GetLanguageDisplayName(string languageCode)
        {
            var languagesList = GetLanguagesList();
            var language = languagesList.FirstOrDefault(l => l.Value == languageCode);
            return language?.Text ?? languageCode; // Return the display name or the code if not found
        }
        private List<SelectListItem> GetPartOfSpeechList()
        {
            return new List<SelectListItem>
                {
    new SelectListItem { Value = "ⲡ", Text = "Masculine (noun) - اسم مذكر " },
    new SelectListItem { Value = "ⲧ", Text = "Feminine (noun) - اسم مؤنث" },
    new SelectListItem { Value = "ⲛ", Text = "Plural (noun) - اسم جمع" },
    new SelectListItem { Value = "ⲟⲩ", Text = "Indefinite noun - اسم غير محدد" },
    new SelectListItem { Value = "ⲣⲁ", Text = "Verb (absolute state) - فعل (صيغة كاملة)" },
    new SelectListItem { Value = "ⲣⲁ-", Text = "Verb (prenominal state) - فعل (صيغة ناقصة)" },
    new SelectListItem { Value = "ⲣⲁ˶", Text = "Verb (prepersona state) - فعل (صيغة ضميرية)" },
    new SelectListItem { Value = "ⲉϥ", Text = "Verb (stative state) - فعل (صيغة وصفية)" },
    new SelectListItem { Value = "ⲣⲁϩ",  Text = "Verb (imperative) - فعل (صيغة أمر)" },
    new SelectListItem { Value = "ⲥ" , Text = "adjective - صفة" },
    new SelectListItem { Value = "ⲡ,ⲧ",  Text = "Masculine or Feminine (noun) - اسم مذكر او مؤنث" },
    new SelectListItem { Value = "ϭⲱⲣ", Text = "Demonstrative pronoun - اسم اشارة" },
    new SelectListItem { Value = "ⲡⲧⲙ", Text = "Relative pronoun - اسم موصول" },
    new SelectListItem { Value = "ϣⲓⲛ",  Text = "interrogative adverb - أداة استفهام" },
    new SelectListItem { Value = "ϣ", Text = "Letter - حرف"  },
    new SelectListItem { Value = "ϣⲣ",  Text = "Conjunction -  حرف عطف" },
    new SelectListItem { Value = "ϣⲥ", Text = "Preposition - حرف جر" },
    new SelectListItem { Value = "ϣϫ",  Text = "negative particle - حرف نفى" },
    new SelectListItem { Value = "ϣⲙ", Text = "direct address marker - حرف نداء" },
    new SelectListItem { Value = "ϣⲃⲣ", Text = "Pronoun - ضمير" },
    new SelectListItem { Value = "ϣⲥⲃ", Text = "Indefinite pronoun - ضمير نكرة" },
    new SelectListItem { Value = "ϣⲁⲫ", Text = "Detached possessive pronoun - ضمير ملكية منفصل" },
    new SelectListItem { Value = "ϣⲁⲧ", Text = "Attached possessive pronoun - ضمير ملكية متصل" },
    new SelectListItem { Value = "ϣⲟⲫ", Text = "Detached personal pronoun - ضمير شخصى منفصل" },
    new SelectListItem { Value = "ϣⲟⲧ", Text = "Attached personal pronoun - ضمير شخصى متصل" },
    new SelectListItem { Value = "ϣⲡ", Text = "First person - ضمير المتكلم" },
    new SelectListItem { Value = "ϣⲡⲛ", Text = "Second person - ضمير المخاطب" },
    new SelectListItem { Value = "ϣⲡⲥ", Text = "Third person - ضمير الغائب" },
    new SelectListItem { Value = "ϣⲛ", Text = "First person plural - ضمير المتكلمين" },
    new SelectListItem { Value = "ϣⲛⲛ", Text = "Second person plural - ضمير المخاطبين" },
    new SelectListItem { Value = "ϣⲛⲥ", Text = "Third person plural - ضمير الغائبين" },
    new SelectListItem { Value = "ⲙⲣ", Text = "Adverb - ظرف" },
    new SelectListItem { Value = "ⲙⲣⲥ", Text = "Adverb of time - ظرف زمان" },
    new SelectListItem { Value = "ⲙⲣⲙ", Text = "Adverb of place - ظرف مكان" },
    new SelectListItem { Value = "ϫϣ", Text = "interjection - صيغة تعجب ، ملاحظة إعتراضية" },
    new SelectListItem { Value = "ⲏⲡⲓ", Text = "Number - عدد" }
};
        }





        [HttpGet]
        public IActionResult Create(string RootSearch = "", string GroupSearch = "")
        {
            var word = new Word(); // Initialize an empty Word object

            // Normalize the input search terms
            RootSearch = NormalizeString(RootSearch);
            GroupSearch = NormalizeString(GroupSearch);

            // Store the search terms in ViewData to repopulate the input fields
            ViewData["RootSearch"] = RootSearch;
            ViewData["GroupSearch"] = GroupSearch;

            // Populate GroupID dropdown with optional search filter
            IEnumerable<GroupWord> groupsQuery = _context.Groups.AsQueryable();
            if (!string.IsNullOrEmpty(GroupSearch))
            {
                groupsQuery = groupsQuery.Where(g => NormalizeString(g.Name).Contains(GroupSearch));
            }

            // Populate the GroupID dropdown with filtered results
            ViewData["GroupID"] = new SelectList(groupsQuery.Select(g => new {
                g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }), "ID", "DisplayField");

            // Retrieve the root words from the database and perform normalization in memory
            IEnumerable<Word> rootWordsQuery = _context.Words.AsEnumerable();  // Use IEnumerable here

            if (!string.IsNullOrEmpty(RootSearch))
            {
                // Apply normalization and filtering in memory
                rootWordsQuery = rootWordsQuery.Where(w => NormalizeString(w.Word_text).Contains(RootSearch));
            }

            // Populate the RootID dropdown with filtered results
            ViewData["RootID"] = new SelectList(rootWordsQuery.Select(w => new {
                w.WordId,
                DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
            }).ToList(), "WordId", "DisplayField");

            // Populate languages for the dropdown
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            return View(word);
        }

        //[HttpGet]
        //public IActionResult Create(string RootSearch = "")
        //{
        //    var word = new Word(); // Initialize an empty Word object



        //    // Populate GroupID dropdown
        //    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new
        //    {
        //        g.ID,
        //        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
        //    }), "ID", "DisplayField");

        //    // Change rootWordsQuery to IEnumerable<Word>


        //    // Populate the root list for the dropdown
        //    ViewData["RootID"] = new SelectList(rootWordsQuery.Select(w => new
        //    {
        //        w.WordId,
        //        DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //    }).ToList(), "WordId", "DisplayField");

        //    // Populate languages for the dropdown
        //    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

        //    return View(word);
        //}






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word, string RootSearch = "", string GroupSearch = "")
        {
            if (ModelState.IsValid)
            {
                _context.Add(word);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = word.WordId });

            }

            // Repopulate dropdowns in case of validation error
            ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
                g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }), "ID", "DisplayField", word.GroupID);

            // Filter words based on RootSearch (and optionally GroupSearch)
            var rootWordsQuery = _context.Words.AsQueryable();

            if (!string.IsNullOrEmpty(RootSearch))
            {
                // Normalize the search term and apply the filter
                string normalizedSearch = NormalizeString(RootSearch.Trim().ToLower());
                rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
            }

            // Filter groups based on GroupSearch
            if (!string.IsNullOrEmpty(GroupSearch))
            {
                rootWordsQuery = rootWordsQuery.Where(w => w.GroupWord.Name.Contains(GroupSearch));
            }

            ViewData["RootID"] = new SelectList(rootWordsQuery.Select(w => new {
                w.WordId,
                DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
            }).ToList(), "WordId", "DisplayField", word.RootID);

            // Populate languages for the dropdown
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            return View(word); // Return the view with the word and populated dropdowns
        }




        [HttpGet]
        public JsonResult SearchWords(string searchTerm)
        {
            // Fetch words based on the search term
            var words = _context.Words
                .Where(w => w.Word_text.Contains(searchTerm))
                .Select(w => new
                {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                })
                .ToList();

            return Json(words);
        }




        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Edit action started for WordId: {WordId}", id);

            // Fetch the word to edit
            var word = await _context.Words.FindAsync(id);
            if (word == null)
            {
                _logger.LogWarning("Word with ID {WordId} not found.", id);
                return NotFound();
            }

            // Populate static dropdowns like languages
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

            // Fetch all roots and groups
            ViewBag.Roots = await _context.Words
                .Select(w => new { WordId = w.WordId, WordText = w.Word_text }) // Use WordId and WordText
                .ToListAsync();

            ViewBag.Groups = await _context.Groups
                .Select(g => new { GroupID = g.ID, GroupName = g.Name }) // Use GroupID and GroupName
                .ToListAsync();

            // Log existing root and group values
            ViewData["RootSearch"] = word.RootID.HasValue
                ? await _context.Words.Where(w => w.WordId == word.RootID)
                    .Select(w => w.Word_text)
                    .FirstOrDefaultAsync()
                : string.Empty;

            ViewData["GroupSearch"] = word.GroupID.HasValue
                ? await _context.Groups.Where(g => g.ID == word.GroupID)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync()
                : string.Empty;
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            _logger.LogInformation("Root and Group values populated for WordId: {WordId}", id);
            return View(word);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
        {
            _logger.LogInformation("Edit POST action started for WordId: {WordId}", id);

            if (id != word.WordId)
            {
                _logger.LogError("Mismatch in WordId: expected {ExpectedId}, received {ReceivedId}", id, word.WordId);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(word);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("WordId {WordId} updated successfully.", id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Words.Any(w => w.WordId == id))
                    {
                        _logger.LogError("WordId {WordId} not found for update.", id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("Details", new { id = word.WordId });

            }
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            // Repopulate dropdowns in case of validation errors
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

            _logger.LogWarning("Validation failed for WordId: {WordId}", id);
            return View(word);
        }

        //var normalizedSearch = NormalizeString(searchTerm);

        //var roots = _context.Words
        //    .Where(w => EF.Functions.Like(NormalizeString(w.Word_text), $"%{normalizedSearch}%"))
        //    .Select(w => new
        //    {
        //        w.WordId,
        //        DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //    })
        //    .ToList();




        // GET: Words/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var word = await _context.Words
                .Include(w => w.GroupWord)
                .Include(w => w.Root)
                .FirstOrDefaultAsync(m => m.WordId == id);
            if (word == null)
            {
                return NotFound();
            }

            return View(word);
        }

        // POST: Words/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var totalWord = await _context.Words.FindAsync(id);
            if (totalWord != null)
            {
                // Find all dependent words that reference this word as their RootID
                var dependentWords = await _context.Words
                    .Where(w => w.RootID == totalWord.WordId)
                    .ToListAsync();

                // Set the RootID of each dependent word to null
                foreach (var word in dependentWords)
                {
                    word.RootID = null;
                }

                // Save changes to the context
                await _context.SaveChangesAsync();

                // Now remove the totalWord entity
                _context.Words.Remove(totalWord);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool WordExists(int id)
        {
            return _context.Words.Any(e => e.WordId == id);
        }




        // GET: CreateWord (For creating a new word and linking it to a specific meaning)
        /// working Creat Word
        //public IActionResult CreateWord(int meaningId)
        //{
        //    TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();
        //    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
        //        g.ID,
        //        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
        //    }), "ID", "DisplayField");

        //    // RootID - Only words that start with "C-"
        //    ViewData["RootID"] = new SelectList(_context.Words
        //        .Where(w => w.Language.StartsWith("C-"))
        //        .Select(w => new {
        //            w.WordId,
        //            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //        }), "WordId", "DisplayField");

        //    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
        //    ViewBag.MeaningId = meaningId; // Pass the MeaningId to the view
        //    return View();
        //}

        // POST: CreateWord (Handles form submission, saves the word, and links it to the meaning)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateWord(int meaningId, [Bind("Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,IsReviewed,RootID,GroupID")] Word word)

        //{
        //    if (ModelState.IsValid)
        //    {
        //        // First, add the word to the database
        //        _context.Words.Add(word);
        //        await _context.SaveChangesAsync();

        //        // Ensure the Word has been successfully saved and has a valid ID
        //        if (word.WordId > 0)
        //        {
        //            // Now, create the WordMeaning relationship and save it
        //            WordMeaning wordMeaning = new WordMeaning
        //            {
        //                WordID = word.WordId,    // This is the correct Word ID
        //                MeaningID = meaningId    // This is the selected Meaning ID
        //            };

        //            _context.WordMeanings.Add(wordMeaning);
        //            await _context.SaveChangesAsync();

        //            // Redirect back to the meaning details page
        //            var returnUrl = TempData["ReturnUrl"] as string;
        //            if (!string.IsNullOrEmpty(returnUrl))
        //            {
        //                return Redirect(returnUrl); // Redirect to the original page
        //            }
        //        }
        //    }

        //    ViewBag.MeaningId = meaningId; // If form is invalid, pass MeaningId again
        //    return View(word);
        //}
        public IActionResult CreateWord(int meaningId)
        {
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            // Populate GroupID dropdown with "No Group" as an option
            var groups = _context.Groups.Select(g => new {
                ID = (int?)g.ID, // Convert to nullable int
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }).ToList();

            groups.Insert(0, new { ID = (int?)null, DisplayField = "No Group" });
            ViewData["GroupID"] = new SelectList(groups, "ID", "DisplayField");

            // Populate RootID dropdown with "No Root" as an option (Only words that start with "C-")
            var roots = _context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    WordId = (int?)w.WordId, // Convert to nullable int
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).ToList();

            roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
            ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField");

            // Populate Languages dropdown
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            // Pass the MeaningId to the view
            ViewBag.MeaningId = meaningId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWord(int meaningId, [Bind("Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,IsReviewed,RootID,GroupID")] Word word)
        {
            if (ModelState.IsValid)
            {
                // If GroupID or RootID is "No Group" or "No Root", set it to null
                if (word.GroupID == 0)
                {
                    word.GroupID = null;
                }

                if (word.RootID == 0)
                {
                    word.RootID = null;
                }

                // First, add the word to the database
                _context.Words.Add(word);
                await _context.SaveChangesAsync();

                // Ensure the Word has been successfully saved and has a valid ID
                if (word.WordId > 0)
                {
                    // Now, create the WordMeaning relationship and save it
                    WordMeaning wordMeaning = new WordMeaning
                    {
                        WordID = word.WordId,    // This is the correct Word ID
                        MeaningID = meaningId    // This is the selected Meaning ID
                    };

                    _context.WordMeanings.Add(wordMeaning);
                    await _context.SaveChangesAsync();

                    // Redirect back to the meaning details page
                    var returnUrl = TempData["ReturnUrl"] as string;
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl); // Redirect to the original page
                    }
                }
            }

            // If form is invalid, pass MeaningId again
            ViewBag.MeaningId = meaningId;

            // Repopulate ViewData for dropdowns if form is invalid
            ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
                g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }).ToList(), "ID", "DisplayField");

            ViewData["RootID"] = new SelectList(_context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).ToList(), "WordId", "DisplayField");

            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(word);
        }

        // GET: SelectFromWord (For selecting an existing word and linking it to a specific meaning)
        // GET: SelectFromWord (Displays the form for selecting an existing word)
        public IActionResult SelectFromWord(int meaningId, int WordId)
        {
            // Pass the list of existing words to the view
            ViewBag.Words = new SelectList(_context.Words.ToList(), "WordId", "Word_text");
            ViewBag.MeaningId = meaningId; // Pass the MeaningId to the view
            ViewBag.WordId = WordId; // Pass the original WordId to the view
            return View();
        }

        // POST: SelectFromWord (Handles the selection of an existing word and links it to the meaning)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectFromWord(int meaningId, int WordID, int WordId)
        {
            if (ModelState.IsValid)
            {
                // Create the WordMeaning relationship
                WordMeaning wordMeaning = new WordMeaning
                {
                    WordID = WordID,  // Link the selected word
                    MeaningID = meaningId // Link the specific meaning
                };
                _context.WordMeanings.Add(wordMeaning);
                await _context.SaveChangesAsync();

                // Redirect to the original word's details page
                return RedirectToAction("Details", "Words", new { id = WordId });
            }

            // If form is invalid, repopulate the words dropdown
            ViewBag.Words = new SelectList(_context.Words.ToList(), "WordId", "Word_text");
            ViewBag.MeaningId = meaningId;
            ViewBag.WordId = WordId;
            return View();
        }


        // GET: Create Example for a specific WordMeaning
        //Working Create Example
        //     public IActionResult CreateExample(int wordMeaningId)
        //     {
        //TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

        //ViewBag.WordMeaningId = wordMeaningId; // Pass the WordMeaningId to the view
        //         return View();
        //     }

        //     // POST: Create Example (Handles form submission, saves example, and links it to the WordMeaning)
        //     [HttpPost]
        //     [ValidateAntiForgeryToken]
        //     public async Task<IActionResult> CreateExample(int wordMeaningId, [Bind("ExampleText,Reference,Pronunciation,Notes")] Example example)
        //     {
        //         if (ModelState.IsValid)
        //         {
        //             // Link the example to the WordMeaning
        //             example.WordMeaningID = wordMeaningId;

        //             // Save the example to the database
        //             _context.Examples.Add(example);
        //             await _context.SaveChangesAsync();

        //	// Redirect back to the WordMeaning details page
        //	var returnUrl = TempData["ReturnUrl"] as string;
        //	if (!string.IsNullOrEmpty(returnUrl))
        //	{
        //		return Redirect(returnUrl); // Redirect to the original page
        //	}
        //}

        //         ViewBag.WordMeaningId = wordMeaningId; // Pass the WordMeaningId again if the form is invalid
        //         return View(example);
        //     }

        public IActionResult CreateExample(int wordMeaningId, int wordId, string language)
        {
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();


            ViewBag.WordMeaningId = wordMeaningId; // Pass the WordMeaningId to the view
            ViewBag.WordId = wordId; // Pass the WordId to the view
            ViewBag.Language = language; // Pass the Language to the view

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExample(int wordMeaningId, int wordId, [Bind("ExampleText,Reference,Pronunciation,Notes")] Example example, string language)
        {
            if (ModelState.IsValid)
            {
                // Link the example to the WordMeaning
                example.WordMeaningID = wordMeaningId;
                example.Language = language; // Set the language from the form

                // Save the example to the database
                _context.Examples.Add(example);
                await _context.SaveChangesAsync();

                // Redirect back to the Word details page
                return RedirectToAction("Details", "Words", new { id = wordId });
            }

            // Pass WordMeaningId and Language again if the form is invalid
            ViewBag.WordMeaningId = wordMeaningId;
            ViewBag.WordId = wordId; // Pass the WordId again
            ViewBag.Language = language; // Pass the language again

            return View(example);
        }
        public async Task<IActionResult> EditExample(int? id)
        {
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the example from the database
            var example = await _context.Examples.FindAsync(id);
            if (example == null)
            {
                return NotFound();
            }

            // Pass the WordMeaningId, WordId, and Language to the view
            ViewBag.WordMeaningId = example.WordMeaningID;
            ViewBag.WordId = example.WordMeaning?.WordID; // Assuming WordMeaning has a WordID property
            ViewBag.Language = example.Language;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(example);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExample(int id, Example example)
        {
            if (id != example.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the example in the database
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

                // Redirect back to the Word details page
                var returnUrl = TempData["ReturnUrl"] as string;
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl); // redirect to the original page
                }
            }

            // If the model state is invalid, pass the WordMeaningId, WordId, and Language again
            ViewBag.WordMeaningId = example.WordMeaningID;
            ViewBag.WordId = example.WordMeaning?.WordID; // Assuming WordMeaning has a WordID property
            ViewBag.Language = example.Language;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(example);
        }


        public async Task<IActionResult> EditChildExample(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the example from the database
            var example = await _context.Examples.FindAsync(id);
            if (example == null)
            {
                return NotFound();
            }

            // Pass the WordMeaningId, WordId, and Language to the view
            ViewBag.WordMeaningId = example.WordMeaningID;
            ViewBag.WordId = example.WordMeaning?.WordID; // Assuming WordMeaning has a WordID property
            ViewBag.Language = example.Language;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(example);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChildExample(int id, Example example)
        {
            if (id != example.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the example in the database
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

                // Redirect back to the Word details page
                return RedirectToAction("Details", "Words", new { id = example.WordMeaning?.WordID });
            }

            // If the model state is invalid, pass the WordMeaningId, WordId, and Language again
            ViewBag.WordMeaningId = example.WordMeaningID;
            ViewBag.WordId = example.WordMeaning?.WordID; // Assuming WordMeaning has a WordID property
            ViewBag.Language = example.Language;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(example);
        }


        // Helper method to check if an Example exists
        private bool ExampleExists(int id)
        {
            return _context.Examples.Any(e => e.ID == id);
        }



        public IActionResult CreateChildExample(int parentExampleId, int wordId)
        {
            // Pass the parent Example ID to the view
            ViewBag.ParentExampleId = parentExampleId;
            ViewBag.WordId = wordId;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChildExample(int parentExampleId, int wordId, [Bind("ExampleText,Reference,Pronunciation,Notes,Language")] Example example)
        {
            if (ModelState.IsValid)
            {
                // Set the parent example for the child
                example.ParentExampleID = parentExampleId;



                // Add the child example to the database
                _context.Examples.Add(example);
                await _context.SaveChangesAsync();

                // Redirect to the parent example's details page or another view
                return RedirectToAction("Details", "Words", new { id = wordId });
            }

            // If the form is invalid, pass the parentExampleId again
            ViewBag.WordId = wordId;
            ViewBag.ParentExampleId = parentExampleId;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(example);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExample(int id)
        {
            var example = await _context.Examples.FindAsync(id);
            if (example != null)
            {
                _context.Examples.Remove(example);
                await _context.SaveChangesAsync();
            }

            // Redirect back to the WordMeaning details page or previous page
            return Redirect(Request.Headers["Referer"].ToString()); // Return to the last page
        }




      
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SelectFromDerivedWord(int WordId1, int RelatedWordID)
        //{
        //    // Check if WordId1 and RelatedWordID exist in the Words table
        //    var word1 = await _context.Words.FindAsync(WordId1);
        //    var word2 = await _context.Words.FindAsync(RelatedWordID);

        //    if (word1 == null || word2 == null)
        //    {
        //        ModelState.AddModelError("", "One of the selected words does not exist.");
        //        // Repopulate the words dropdown and pass back to the view
        //        ViewBag.Words = new SelectList(
        //            _context.Words.Where(w => w.WordId != WordId1).ToList(),
        //            "WordId",
        //            "Word_text"
        //        );
        //        ViewBag.WordId1 = WordId1;
        //        return View();
        //    }

        //    // Prevent linking a word to itself
        //    if (WordId1 == RelatedWordID)
        //    {
        //        ModelState.AddModelError("", "A word cannot be linked to itself.");
        //        ViewBag.Words = new SelectList(
        //            _context.Words.Where(w => w.WordId != WordId1).ToList(),
        //            "WordId",
        //            "Word_text"
        //        );
        //        ViewBag.WordId1 = WordId1;
        //        return View();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        // Check if the relationship already exists
        //        var existingLink = await _context.DrevWords
        //            .FirstOrDefaultAsync(dw => dw.WordID == WordId1 && dw.RelatedWordID == RelatedWordID);

        //        if (existingLink != null)
        //        {
        //            ModelState.AddModelError("", "This relationship already exists.");
        //            ViewBag.Words = new SelectList(
        //                _context.Words.Where(w => w.WordId != WordId1).ToList(),
        //                "WordId",
        //                "Word_text"
        //            );
        //            ViewBag.WordId1 = WordId1;
        //            return View();
        //        }

        //        // Create the DrevWord relationship
        //        DrevWord drevWord = new DrevWord
        //        {
        //            WordID = WordId1,
        //            RelatedWordID = RelatedWordID
        //        };

        //        // Add the relationship to the database
        //        _context.DrevWords.Add(drevWord);
        //        await _context.SaveChangesAsync();

        //        // Redirect to the word details page
        //        return RedirectToAction("Details", "Words", new { id = WordId1 });
        //    }

        //    // Repopulate the dropdown if the model is invalid
        //    ViewBag.Words = new SelectList(
        //        _context.Words.Where(w => w.WordId != WordId1).ToList(),
        //        "WordId",
        //        "Word_text"
        //    );
        //    ViewBag.WordId1 = WordId1;
        //    return View();
        //}





        // POST: Handle the form submission, search for the Bible verse, and save it
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateBibleReference(int wordMeaningId, string book, int chapter, int verse, string language, string Edition)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Search for the Bible verse based on user input
        //        var bibleVerse = await _context.Bibles
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(b => b.Book == book
        //                                      && b.Chapter == chapter
        //                                      && b.Verse == verse
        //                                      && b.Language == language
        //                                      && b.Edition == Edition);

        //        if (bibleVerse != null)
        //        {
        //            // Create a new WordMeaningBible linking the found Bible verse to the WordMeaning
        //            var wordMeaningBible = new WordMeaningBible
        //            {
        //                WordMeaningID = wordMeaningId,
        //                BibleID = bibleVerse.BibleID
        //            };

        //            // Save the WordMeaningBible to the database
        //            _context.WordMeaningBibles.Add(wordMeaningBible);
        //            await _context.SaveChangesAsync();

        //            // Redirect back to the WordMeaning details page
        //            var returnUrl = TempData["ReturnUrl"] as string;
        //            if (!string.IsNullOrEmpty(returnUrl))
        //            {
        //                return Redirect(returnUrl); // Redirect to the original page
        //            }
        //        }
        //        else
        //        {
        //            // If no verse is found, display an error message
        //            ModelState.AddModelError("", "Bible verse not found.");
        //        }
        //    }

        //    ViewBag.WordMeaningId = wordMeaningId; // Pass the WordMeaningId again if the form is invalid
        //    return View();
        //}
public IActionResult CreateBibleReference(int wordMeaningId)
{
    TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();
    
    // Get the word meaning with the associated word to extract the language
    var wordMeaning = _context.WordMeanings
        .Include(wm => wm.Word)
        .FirstOrDefault(wm => wm.ID == wordMeaningId);
    
    if (wordMeaning == null)
    {
        return NotFound("Word meaning not found.");
    }
    
    ViewBag.WordMeaningId = wordMeaningId;
    ViewBag.WordLanguage = wordMeaning.Word.Language; // Pass the word's language
    ViewBag.WordText = wordMeaning.Word.Word_text; // Optional: for display context
    
    // Add Bible books dropdown with full Arabic names
    ViewBag.BibleBooks = GetBibleBooksSelectList("AR", includeFullNames: true);
    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

    return View();
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBibleReference(int wordMeaningId, int bookNumber, int chapter, int verse, string language, string Edition)
        {
            if (ModelState.IsValid)
            {
                // Search for the Bible verse using optimized query
                var bibleVerseId = await _context.Bibles
                    .AsNoTracking()  // Disable change tracking
                    .Where(b => b.Book == bookNumber
                                && b.Chapter == chapter
                                && b.Verse == verse
                                && b.Language == language
                                && b.Edition == Edition)
                    .Select(b => b.BibleID) // Only select the BibleID to improve performance
                    .FirstOrDefaultAsync();

                if (bibleVerseId != 0) // 0 means no verse was found
                {
                    // Check if this reference already exists
                    var existingReference = await _context.WordMeaningBibles
                        .FirstOrDefaultAsync(wmb => wmb.WordMeaningID == wordMeaningId && wmb.BibleID == bibleVerseId);

                    if (existingReference == null)
                    {
                        // Create and save the WordMeaningBible
                        var wordMeaningBible = new WordMeaningBible
                        {
                            WordMeaningID = wordMeaningId,
                            BibleID = bibleVerseId
                        };

                        _context.WordMeaningBibles.Add(wordMeaningBible);
                        await _context.SaveChangesAsync();

                        // Redirect to the original page
                        var returnUrl = TempData["ReturnUrl"] as string;
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "This Bible reference already exists for this word meaning.");
                    }
                }
                else
                {
                    var bookName = GetBibleBookDisplayName(bookNumber, "AR", useFullName: true);
                    var languageName = GetLanguageDisplayName(language);
                    ModelState.AddModelError("", $"Bible verse not found: {bookName} {chapter}:{verse} in {languageName} ({Edition})");
                }
            }

            // Repopulate dropdowns if there's an error
            ViewBag.WordMeaningId = wordMeaningId;
            ViewBag.BibleBooks = GetBibleBooksSelectList("AR", includeFullNames: true);
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", language);

            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBibleReference(int id)
        {
            var BibleReference = await _context.WordMeaningBibles.FindAsync(id);
            if (BibleReference != null)
            {
                _context.WordMeaningBibles.Remove(BibleReference);
                await _context.SaveChangesAsync();
            }

            // Redirect back to the WordMeaning details page or previous page
            return Redirect(Request.Headers["Referer"].ToString()); // Return to the last page
        }


        public IActionResult AddChildMeaning(int parentMeaningId, int wordId)
        {
            ViewBag.WordId = wordId;
            ViewBag.ParentMeaningId = parentMeaningId;
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChildMeaning(int parentMeaningId, int wordId, string meaningText, string? notes, string? language)
        {
            if (ModelState.IsValid)
            {
                // Create a new child meaning with the provided data
                var childMeaning = new Meaning
                {
                    MeaningText = meaningText,
                    Notes = notes,
                    Language = language,
                    ParentMeaningID = parentMeaningId
                };

                _context.Meanings.Add(childMeaning);
                await _context.SaveChangesAsync();

                // Redirect back to the parent meaning details page (or wherever appropriate)
                return RedirectToAction("Details", "Words", new { id = wordId });

            }

            // Pass the parentMeaningId back to the view if something goes wrong
            ViewBag.WordId = wordId;
            ViewBag.ParentMeaningId = parentMeaningId;
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteChildMeaning(int id, int wordId)
        //{
        //    var meaning = await _context.Meanings.FindAsync(id);
        //    if (meaning == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Meanings.Remove(meaning);
        //    await _context.SaveChangesAsync();

        //    // Redirect to the same page or any other view after deletion
        //    return RedirectToAction("Details", "Words", new { id = wordId });
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChildMeaning(int id, int wordId)
        {

            var meaning = await _context.Meanings.FindAsync(id);

            if (meaning == null)
            {
                Console.WriteLine($"Meaning with ID {id} was not found.");
                return NotFound();
            }

            try
            {
                _context.Meanings.Remove(meaning);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting meaning with ID {id}: {ex.Message}");
                return BadRequest("Error occurred while deleting.");
            }

            return RedirectToAction("Details", "Words", new { id = wordId });
        }





        public IActionResult CreateGroupAsChild(int parentGroupID, int wordId)
        {
            var model = new CreateGroupViewModel
            {
                ParentGroupID = parentGroupID
            };
            ViewBag.WordId = wordId;
            return View(model);
        }

        // Action to handle form submission
        [HttpPost]
        public async Task<IActionResult> CreateGroupAsChild(CreateGroupViewModel model, int wordId)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create a new GroupWord
            var newGroup = new GroupWord
            {
                Name = model.NewGroupName,
                OriginLanguage = model.OriginLanguage,
                EtymologyWord = model.EtymologyWord,
                Etymology = model.Etymology,
                Notes = model.Notes
            };

            // Add the new GroupWord to the context
            _context.Groups.Add(newGroup);
            await _context.SaveChangesAsync(); // Save to get the new Group ID

            // Create the GroupRelation
            var groupRelation = new GroupRelation
            {
                ParentGroupID = model.ParentGroupID,
                RelatedGroupID = newGroup.ID,
                IsCompound = model.IsCompound
            };

            // Add the GroupRelation to the context
            _context.GroupRelations.Add(groupRelation);
            await _context.SaveChangesAsync(); // Save the relation

            return RedirectToAction("Details", "Words", new { id = wordId });
        }




        public IActionResult CreateGroupAsParent(int ChildGroupID, int wordId)
        {
            var model = new CreateGroupViewModel
            {
                ParentGroupID = ChildGroupID
            };
            ViewBag.wordId = wordId;
            return View(model);
        }

        // Action to handle form submission
        [HttpPost]
        public async Task<IActionResult> CreateGroupAsParent(CreateGroupViewModel model, int wordId)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create a new GroupWord
            var newGroup = new GroupWord
            {
                Name = model.NewGroupName,
                OriginLanguage = model.OriginLanguage,
                EtymologyWord = model.EtymologyWord,
                Etymology = model.Etymology,
                Notes = model.Notes
            };

            // Add the new GroupWord to the context
            _context.Groups.Add(newGroup);
            await _context.SaveChangesAsync(); // Save to get the new Group ID

            // Create the GroupRelation
            var groupRelation = new GroupRelation
            {
                ParentGroupID = newGroup.ID,
                RelatedGroupID = model.ParentGroupID,
                IsCompound = model.IsCompound
            };

            // Add the GroupRelation to the context
            _context.GroupRelations.Add(groupRelation);
            await _context.SaveChangesAsync(); // Save the relation
            return RedirectToAction("Details", "Words", new { id = wordId });

        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroupRelation(int id, int wordId)
        {

            var relation = await _context.GroupRelations.FindAsync(id);

            if (relation == null)
            {
                Console.WriteLine($"relation with ID {id} was not found.");
                return NotFound();
            }

            try
            {
                _context.GroupRelations.Remove(relation);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting meaning with ID {id}: {ex.Message}");
                return BadRequest("Error occurred while deleting.");
            }

            return RedirectToAction("Details", "Words", new { id = wordId });
        }




        public IActionResult AddMeaningToGroup(int groupId, int wordId)
        {
            var model = new AddMeaningToGroupViewModel
            {
                GroupId = groupId
            };
            ViewBag.wordId = wordId;

            return View(model);
        }

        // POST Action to handle the form submission
        [HttpPost]
        public async Task<IActionResult> AddMeaningToGroup(AddMeaningToGroupViewModel model, int wordId)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get all words in the specified group
            var words = await _context.Words
                .Where(w => w.GroupID == model.GroupId)
                .ToListAsync();

            if (words == null || !words.Any())
            {
                TempData["Error"] = "No words found in the specified group.";
                return RedirectToAction("Details", "GroupWord", new { id = model.GroupId });
            }

            // Create a new meaning
            var newMeaning = new Meaning
            {
                MeaningText = model.MeaningText,
                Language = model.Language,
                Notes = model.Notes
            };

            // Add the new meaning to each word in the group
            foreach (var word in words)
            {
                var wordMeaning = new WordMeaning
                {
                    WordID = word.WordId,
                    Meaning = newMeaning
                };
                _context.WordMeanings.Add(wordMeaning);
            }

            // Save the changes to the database
            await _context.SaveChangesAsync();

            TempData["Message"] = "Meaning added to all words in the group successfully.";
            return RedirectToAction("Details", "Words", new { id = wordId });
        }






        public IActionResult AddWordToGroup(int groupid)
        {
            // GroupID - Combine Name, OriginLanguage, and EtymologyWord for the display field
            ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
                g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }), "ID", "DisplayField");

            // RootID - Only words that start with "C-"
            ViewData["RootID"] = new SelectList(_context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }), "WordId", "DisplayField");

            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            ViewBag.GroupID = groupid;
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWordToGroup(int groupid, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
        {
            if (ModelState.IsValid)
            {
                _context.Add(word);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // GroupID - Combine Name, OriginLanguage, and EtymologyWord for the display field
            ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
                g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }), "ID", "DisplayField", word.GroupID);

            // RootID - Only words that start with "C-"
            ViewData["RootID"] = new SelectList(_context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }), "WordId", "DisplayField", word.RootID);

            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
            var returnUrl = TempData["ReturnUrl"] as string;
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return View(word);
        }



        // GET: Words/AddDictionaryReference/5
        public IActionResult AddDictionaryReference(int wordId)
        {
            var word = _context.Words.Find(wordId);
            if (word == null)
            {
                return NotFound();
            }

            ViewBag.WordId = wordId;
            ViewBag.Dictionaries = _context.Dictionaries.ToList();
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        // POST: Words/AddDictionaryReference
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDictionaryReference(int wordId, DictionaryReferenceWord dictionaryReferenceWord)
        {
            if (ModelState.IsValid)
            {
                dictionaryReferenceWord.WordID = wordId;
                _context.DictionaryReferenceWords.Add(dictionaryReferenceWord);
                _context.SaveChanges();
                return RedirectToAction(nameof(Details), new { id = wordId });
            }

            ViewBag.WordId = wordId;
            ViewBag.Dictionaries = _context.Dictionaries.ToList();
            return View(dictionaryReferenceWord);
        }

        // GET: Words/EditDictionaryReference/5
        public IActionResult EditDictionaryReference(int id)
        {
            var dictionaryReference = _context.DictionaryReferenceWords
                .Include(d => d.Dictionary)
                .Include(d => d.Word)
                .FirstOrDefault(d => d.ID == id);

            if (dictionaryReference == null)
            {
                return NotFound();
            }

            ViewBag.Dictionaries = _context.Dictionaries.ToList();
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(dictionaryReference);
        }

        // POST: Words/EditDictionaryReference/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDictionaryReference(int id, DictionaryReferenceWord dictionaryReferenceWord)
        {
            if (id != dictionaryReferenceWord.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(dictionaryReferenceWord);
                _context.SaveChanges();
                return RedirectToAction(nameof(Details), new { id = dictionaryReferenceWord.WordID });
            }

            ViewBag.Dictionaries = _context.Dictionaries.ToList();
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(dictionaryReferenceWord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDictionaryReference(int id)
        {
            var dictionaryReference = _context.DictionaryReferenceWords.Find(id);
            if (dictionaryReference == null)
            {
                return NotFound();
            }

            var wordId = dictionaryReference.WordID; // Save WordID for redirection
            _context.DictionaryReferenceWords.Remove(dictionaryReference);
            _context.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = wordId });
        }

        // GET: WordExplanations/CreateWordExplanation
        public IActionResult CreateWordExplanation(int wordId)
        {
            var wordExplanation = new WordExplanation { WordID = wordId };
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(wordExplanation);
        }

        // POST: WordExplanations/CreateWordExplanation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWordExplanation(WordExplanation wordExplanation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wordExplanation);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Words", new { id = wordExplanation.WordID });
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(wordExplanation);
        }

        // GET: WordExplanations/EditWordExplanation/5
        public async Task<IActionResult> EditWordExplanation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordExplanation = await _context.WordExplanations.FindAsync(id);
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            if (wordExplanation == null)
            {
                return NotFound();
            }
            return View(wordExplanation);
        }

        // POST: WordExplanations/EditWordExplanation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWordExplanation(int id, WordExplanation wordExplanation)
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
                return RedirectToAction("Details", "Words", new { id = wordExplanation.WordID });
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

            return View(wordExplanation);
        }

        // GET: WordExplanations/DeleteWordExplanation/5
        public async Task<IActionResult> DeleteWordExplanation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wordExplanation = await _context.WordExplanations
                .Include(we => we.Word)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (wordExplanation == null)
            {
                return NotFound();
            }

            return View(wordExplanation);
        }

        // POST: WordExplanations/DeleteWordExplanation/5
        [HttpPost, ActionName("DeleteWordExplanation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWordExplanationConfirmed(int id)
        {
            var wordExplanation = await _context.WordExplanations.FindAsync(id);
            if (wordExplanation != null)
            {
                _context.WordExplanations.Remove(wordExplanation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Words", new { id = wordExplanation.WordID });
        }

        private bool WordExplanationExists(int id)
        {
            return _context.WordExplanations.Any(e => e.ID == id);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPronunciation(int wordId, IFormFile audioFile)
        {
            try
            {
                _logger.LogInformation("Upload pronunciation request for word {WordId}", wordId);

                if (audioFile == null || audioFile.Length == 0)
                {
                    _logger.LogWarning("No audio file provided for word {WordId}", wordId);
                    return Json(new { success = false, message = "No audio file provided" });
                }

                var word = await _context.Words.FindAsync(wordId);
                if (word == null)
                {
                    _logger.LogWarning("Word not found: {WordId}", wordId);
                    return Json(new { success = false, message = "Word not found" });
                }

                // Check file size (limit to 10MB)
                if (audioFile.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File too large. Maximum size is 10MB." });
                }

                // Check file type
                var allowedTypes = new[] { "audio/wav", "audio/mpeg", "audio/mp3", "audio/ogg" };
                if (!allowedTypes.Contains(audioFile.ContentType.ToLower()))
                {
                    return Json(new { success = false, message = "Invalid file type. Please upload an audio file." });
                }

                // Generate unique filename
                var fileName = $"pronunciation_{wordId}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";

                _logger.LogInformation("Uploading file {FileName} for word {WordId}", fileName, wordId);

                // Upload to Google Drive
                using (var stream = audioFile.OpenReadStream())
                {
                    var fileId = await _googleDriveService.UploadAudioFileAsync(stream, fileName);
                    var publicLink = await _googleDriveService.GetPublicLinkAsync(fileId);

                    // Update word with pronunciation link
                    word.Pronunciation = publicLink;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pronunciation saved successfully for word {WordId}", wordId);
                    return Json(new { success = true, pronunciationUrl = publicLink });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pronunciation for word {WordId}", wordId);
                return Json(new { success = false, message = $"Error uploading pronunciation: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeletePronunciation(int wordId)
        {
            try
            {
                var word = await _context.Words.FindAsync(wordId);
                if (word == null)
                {
                    return Json(new { success = false, message = "Word not found" });
                }

                if (!string.IsNullOrEmpty(word.Pronunciation))
                {
                    // Extract file ID from Google Drive URL
                    var fileId = ExtractFileIdFromUrl(word.Pronunciation);
                    if (!string.IsNullOrEmpty(fileId))
                    {
                        await _googleDriveService.DeleteFileAsync(fileId);
                    }

                    word.Pronunciation = null;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pronunciation for word {WordId}", wordId);
                return Json(new { success = false, message = "Error deleting pronunciation" });
            }
        }


       // GET: Words/ExampleDetails/5
public async Task<IActionResult> ExampleDetails(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var example = await _context.Examples
        .Include(e => e.WordMeaning)
            .ThenInclude(wm => wm.Word)
        .Include(e => e.WordMeaning)
            .ThenInclude(wm => wm.Meaning)
        .Include(e => e.ParentExample)
        .Include(e => e.ChildExamples)
        .FirstOrDefaultAsync(e => e.ID == id);

    if (example == null)
    {
        return NotFound();
    }

    return View(example);
}

// POST: Words/DeleteExamplePronunciation
[HttpPost]
public async Task<IActionResult> DeleteExamplePronunciation(int exampleId)
{
    try
    {
        var example = await _context.Examples.FindAsync(exampleId);
        if (example == null)
        {
            return Json(new { success = false, message = "Example not found" });
        }

        if (!string.IsNullOrEmpty(example.Pronunciation))
        {
            // Extract file ID from Google Drive URL
            var fileId = ExtractFileIdFromUrl(example.Pronunciation);
            if (!string.IsNullOrEmpty(fileId))
            {
                await _googleDriveService.DeleteFileAsync(fileId);
            }

            example.Pronunciation = null;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting pronunciation for example {ExampleId}", exampleId);
        return Json(new { success = false, message = "Error deleting pronunciation" });
    }
}

        [HttpPost]
        public async Task<IActionResult> UploadExampleAudio(IFormFile audioFile, int exampleId)
        {
            try
            {
                _logger.LogInformation("Upload example audio file request for example {ExampleId}", exampleId);

                if (audioFile == null || audioFile.Length == 0)
                {
                    _logger.LogWarning("No audio file provided for example {ExampleId}", exampleId);
                    return Json(new { success = false, message = "No audio file provided" });
                }

                var example = await _context.Examples.FindAsync(exampleId);
                if (example == null)
                {
                    _logger.LogWarning("Example not found: {ExampleId}", exampleId);
                    return Json(new { success = false, message = "Example not found" });
                }

                // Check file size (limit to 10MB)
                if (audioFile.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File too large. Maximum size is 10MB." });
                }

                // Generate unique filename with WAV extension
                var fileName = $"example_audio_{exampleId}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";

                _logger.LogInformation("Uploading WAV file {FileName} for example {ExampleId}", fileName, exampleId);

                // Upload to Google Drive
                using (var stream = audioFile.OpenReadStream())
                {
                    var fileId = await _googleDriveService.UploadAudioFileAsync(stream, fileName);
                    var publicLink = await _googleDriveService.GetPublicLinkAsync(fileId);

                    // Update example with pronunciation link
                    example.Pronunciation = publicLink;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pronunciation saved successfully for example {ExampleId}", exampleId);
                    return Json(new { success = true, pronunciationUrl = publicLink });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pronunciation for example {ExampleId}", exampleId);
                return Json(new { success = false, message = $"Error uploading pronunciation: {ex.Message}" });
            }
        }


        [HttpPost]
public async Task<IActionResult> DeleteExampleAudio(int exampleId)
{
    try
    {
        var example = await _context.Examples.FindAsync(exampleId);
        if (example == null)
        {
            return Json(new { success = false, message = "Example not found" });
        }

        if (!string.IsNullOrEmpty(example.Pronunciation))
        {
            // Extract file ID from Google Drive URL
            var fileId = ExtractFileIdFromUrl(example.Pronunciation);
            if (!string.IsNullOrEmpty(fileId))
            {
                await _googleDriveService.DeleteFileAsync(fileId);
            }

            // Update the example to remove the pronunciation URL
            example.Pronunciation = null;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting example audio for example {ExampleId}", exampleId);
        return Json(new { success = false, message = "Error deleting audio" });
    }
}

        private string ExtractFileIdFromUrl(string googleDriveUrl)
        {
            if (string.IsNullOrEmpty(googleDriveUrl))
                return null;

            // Extract file ID from Google Drive URL pattern
            var match = System.Text.RegularExpressions.Regex.Match(googleDriveUrl, @"/file/d/([a-zA-Z0-9-_]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
        // ...existing code...

        [HttpGet]
        public async Task<IActionResult> TestGoogleDrive()
        {
            try
            {
                // Create a test stream
                var testContent = "Hello, Google Drive!";
                var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));

                var fileId = await _googleDriveService.UploadAudioFileAsync(testStream, "test.txt");
                return Json(new { success = true, fileId = fileId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ...existing code...

        // GET: SelectWordForMeaning (For selecting an existing word and linking it to a specific meaning)
        public IActionResult SelectWordForMeaning(int meaningId, int currentWordId, string? search, string? searchType = "start")
        {
            // Get the meaning to display context
            var meaning = _context.Meanings
                .Include(m => m.WordMeanings)
                .ThenInclude(wm => wm.Word)
                .FirstOrDefault(m => m.ID == meaningId);

            if (meaning == null)
            {
                return NotFound();
            }

            // Get words already linked to this meaning to exclude them from selection
            var linkedWordIds = meaning.WordMeanings.Select(wm => wm.WordID).ToList();

            // Get all available words (excluding already linked ones)
            var availableWordsQuery = _context.Words
                .Where(w => !linkedWordIds.Contains(w.WordId))
                .AsQueryable();

            var availableWordsList = availableWordsQuery.ToList();

            // Apply search filtering if search term is provided
            if (!string.IsNullOrEmpty(search))
            {
                // Normalize the search string
                search = NormalizeString(search);

                // Apply the search type
                switch (searchType)
                {
                    case "exact":
                        // Exact match
                        availableWordsList = availableWordsList.Where(w => NormalizeString(w.Word_text) == search).ToList();
                        break;

                    case "contain":
                        // Contains search term
                        availableWordsList = availableWordsList.Where(w => NormalizeString(w.Word_text).Contains(search)).ToList();
                        break;

                    case "start":
                        // Starts with search term
                        availableWordsList = availableWordsList.Where(w => NormalizeString(w.Word_text).StartsWith(search)).ToList();
                        break;

                    case "end":
                        // Ends with search term
                        availableWordsList = availableWordsList.Where(w => NormalizeString(w.Word_text).EndsWith(search)).ToList();
                        break;

                    default:
                        // Default to starts with search if no valid search type is provided
                        availableWordsList = availableWordsList.Where(w => NormalizeString(w.Word_text).StartsWith(search)).ToList();
                        break;
                }
            }

            var selectListItems = availableWordsList.Select(w => new SelectListItem
            {
                Value = w.WordId.ToString(),
                Text = $"{w.Word_text} ({w.Language}) - {w.Class}"
            }).ToList();

            ViewBag.Words = new SelectList(selectListItems, "Value", "Text");
            ViewBag.MeaningId = meaningId;
            ViewBag.CurrentWordId = currentWordId;
            ViewBag.MeaningText = meaning.MeaningText;
            ViewBag.SearchText = search;
            ViewBag.SearchType = searchType;

            return View();
        }

        // POST: SelectWordForMeaning (Handles the selection and creates the WordMeaning relationship)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectWordForMeaning(int meaningId, int selectedWordId, int currentWordId, string? search, string? searchType)
        {
            // Validate that the meaning exists
            var meaningExists = await _context.Meanings.AnyAsync(m => m.ID == meaningId);
            if (!meaningExists)
            {
                ModelState.AddModelError("", "The selected meaning does not exist.");
                return RedirectToAction("Details", new { id = currentWordId });
            }

            // Validate that the word exists
            var wordExists = await _context.Words.AnyAsync(w => w.WordId == selectedWordId);
            if (!wordExists)
            {
                ModelState.AddModelError("", "The selected word does not exist.");
                return RedirectToAction("Details", new { id = currentWordId });
            }

            // Check if this word-meaning relationship already exists
            var existingRelationship = await _context.WordMeanings
                .AnyAsync(wm => wm.WordID == selectedWordId && wm.MeaningID == meaningId);

            if (existingRelationship)
            {
                // If relationship already exists, just redirect back
                return RedirectToAction("Details", new { id = currentWordId });
            }

            if (ModelState.IsValid)
            {
                // Create new word-meaning relationship
                WordMeaning wordMeaning = new WordMeaning
                {
                    WordID = selectedWordId,
                    MeaningID = meaningId
                };

                _context.WordMeanings.Add(wordMeaning);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = currentWordId });
            }

            return RedirectToAction("Details", new { id = currentWordId });
        }

        // ...existing code...
        [HttpGet]
        public async Task<IActionResult> SearchRoots(string searchTerm)
        {
            try
            {
                var rootsQuery = _context.Words
                    .Where(w => w.Language.StartsWith("C-"))
                    .AsQueryable();

                var allRoots = await rootsQuery.ToListAsync();

                // Apply search filtering if search term is provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var normalizedSearch = NormalizeString(searchTerm);
                    allRoots = allRoots.Where(w => NormalizeString(w.Word_text).Contains(normalizedSearch)).ToList();
                }

                var results = allRoots.Select(w => new {
                    value = w.WordId,
                    text = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).Take(50).ToList(); // Limit to 50 results for performance

                return Json(results);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchGroups(string searchTerm)
        {
            try
            {
                var groupsQuery = _context.Groups.AsQueryable();
                var allGroups = await groupsQuery.ToListAsync();

                // Apply search filtering if search term is provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var normalizedSearch = NormalizeString(searchTerm);
                    allGroups = allGroups.Where(g => NormalizeString(g.Name).Contains(normalizedSearch) ||
                                                   NormalizeString(g.OriginLanguage ?? "").Contains(normalizedSearch) ||
                                                   NormalizeString(g.EtymologyWord ?? "").Contains(normalizedSearch)).ToList();
                }

                var results = allGroups.Select(g => new {
                    value = g.ID,
                    text = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
                }).Take(50).ToList(); // Limit to 50 results for performance

                return Json(results);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }



        // ...existing code...

        // GET: CreateWordAddedToMeaning (For creating a new word and linking it to a specific meaning)
        public IActionResult CreateWordAddedToMeaning(int meaningId)
        {
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            // Populate GroupID dropdown with "No Group" as an option
            var groups = _context.Groups.Select(g => new {
                ID = (int?)g.ID, // Convert to nullable int
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }).ToList();

            groups.Insert(0, new { ID = (int?)null, DisplayField = "No Group" });
            ViewData["GroupID"] = new SelectList(groups, "ID", "DisplayField");

            // Populate RootID dropdown with "No Root" as an option (Only words that start with "C-")
            var roots = _context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    WordId = (int?)w.WordId, // Convert to nullable int
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).ToList();

            roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
            ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField");

            // Populate Languages dropdown
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            // Pass the MeaningId to the view
            ViewBag.MeaningId = meaningId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWordAddedToMeaning(int meaningId, [Bind("Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,IsReviewed,RootID,GroupID")] Word word)
        {
            if (ModelState.IsValid)
            {
                // If GroupID or RootID is "No Group" or "No Root", set it to null
                if (word.GroupID == 0)
                {
                    word.GroupID = null;
                }

                if (word.RootID == 0)
                {
                    word.RootID = null;
                }

                // First, add the word to the database
                _context.Words.Add(word);
                await _context.SaveChangesAsync();

                // Ensure the Word has been successfully saved and has a valid ID
                if (word.WordId > 0)
                {
                    // Now, create the WordMeaning relationship and save it
                    WordMeaning wordMeaning = new WordMeaning
                    {
                        WordID = word.WordId,    // This is the newly created Word ID
                        MeaningID = meaningId    // This is the selected Meaning ID
                    };

                    _context.WordMeanings.Add(wordMeaning);
                    await _context.SaveChangesAsync();

                    // Redirect back to the original page
                    var returnUrl = TempData["ReturnUrl"] as string;
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl); // Redirect to the original page
                    }

                    // Fallback: redirect to the new word's details page
                    return RedirectToAction("Details", new { id = word.WordId });
                }
            }

            // If form is invalid, pass MeaningId again and repopulate dropdowns
            ViewBag.MeaningId = meaningId;

            // Repopulate ViewData for dropdowns if form is invalid
            var groups = _context.Groups.Select(g => new {
                ID = (int?)g.ID,
                DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
            }).ToList();
            groups.Insert(0, new { ID = (int?)null, DisplayField = "No Group" });
            ViewData["GroupID"] = new SelectList(groups, "ID", "DisplayField");

            var roots = _context.Words
                .Where(w => w.Language.StartsWith("C-"))
                .Select(w => new {
                    WordId = (int?)w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).ToList();
            roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
            ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField");

            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            return View(word);
        }

        // ...existing code...


        private List<BibleBookInfo> GetBibleBooksList()
        {
            return new List<BibleBookInfo>
  {
        new BibleBookInfo { BookNumber = 1, EN = "Gen", AR = "تك", CB = "ⲅⲉⲛ", CS = "ⲅⲉⲛ", ARFull = "تكوين" },
        new BibleBookInfo { BookNumber = 2, EN = "Exo", AR = "خر", CB = "ⲉⲝ", CS = "ⲉⲝ", ARFull = "خروج" },
        new BibleBookInfo { BookNumber = 3, EN = "Lev", AR = "لا", CB = "ⲗⲉⲩ", CS = "ⲗⲉⲩ", ARFull = "لاويين" },
        new BibleBookInfo { BookNumber = 4, EN = "Num", AR = "عد", CB = "ⲁⲣ", CS = "ⲁⲣ", ARFull = "العدد" },
        new BibleBookInfo { BookNumber = 5, EN = "Deut", AR = "تث", CB = "ⲇⲉⲩ", CS = "ⲇⲉⲩ", ARFull = "التثنية" },
        new BibleBookInfo { BookNumber = 6, EN = "Josh", AR = "يش", CB = "ⲓⲏ", CS = "ⲓⲏ", ARFull = "يشوع" },
        new BibleBookInfo { BookNumber = 7, EN = "Judg", AR = "قض", CB = "ⲕⲣ", CS = "ⲕⲣ", ARFull = "القضاة" },
        new BibleBookInfo { BookNumber = 8, EN = "Ruth", AR = "را", CB = "ⲣⲱ", CS = "ⲣⲱ", ARFull = "راعوث" },
        new BibleBookInfo { BookNumber = 9, EN = "1Sam", AR = "1صم", CB = "ⲁ̅ ⲥⲁⲙ", CS = "ⲁ̅ ⲥⲁⲙ", ARFull = "صموئيل الأول" },
        new BibleBookInfo { BookNumber = 10, EN = "2Sam", AR = "2صم", CB = "ⲃ̅ ⲥⲁⲙ", CS = "ⲃ̅ ⲥⲁⲙ", ARFull = "صموئيل الثاني" },
        new BibleBookInfo { BookNumber = 11, EN = "1Kgs", AR = "1مل", CB = "ⲁ̅ ⲃⲁⲥ", CS = "ⲁ̅ ⲃⲁⲥ", ARFull = "الملوك الأول" },
        new BibleBookInfo { BookNumber = 12, EN = "2Kgs", AR = "2مل", CB = "ⲃ̅ ⲃⲁⲥ", CS = "ⲃ̅ ⲃⲁⲥ", ARFull = "الملوك الثاني" },
        new BibleBookInfo { BookNumber = 13, EN = "1Chr", AR = "1اخ", CB = "ⲁ̅ ⲡⲁⲣⲁ", CS = "ⲁ̅ ⲡⲁⲣⲁ", ARFull = "أخبار الأيام الأول" },
        new BibleBookInfo { BookNumber = 14, EN = "2Chr", AR = "2اخ", CB = "ⲃ̅ ⲡⲁⲣⲁ", CS = "ⲃ̅ ⲡⲁⲣⲁ", ARFull = "أخبار الأيام الثاني" },
        new BibleBookInfo { BookNumber = 15, EN = "Pr. of Man", AR = "صلاة منسى", CB = "ⲠⲣⲘⲛⲥ", CS = "ⲠⲣⲘⲛⲥ", ARFull = "صلاة منسى" },
        new BibleBookInfo { BookNumber = 16, EN = "Ezra", AR = "عز", CB = "ⲉⲥⲇ", CS = "ⲉⲥⲇ", ARFull = "عزرا" },
        new BibleBookInfo { BookNumber = 17, EN = "Neh", AR = "نح", CB = "ⲛⲉ", CS = "ⲛⲉ", ARFull = "نحميا" },
        new BibleBookInfo { BookNumber = 18, EN = "Tob", AR = "طو", CB = "ⲧⲱⲃϩ", CS = "ⲧⲱⲃϩ", ARFull = "طوبيا" },
        new BibleBookInfo { BookNumber = 19, EN = "Jdth", AR = "يهو", CB = "ⲓⲟⲩⲇ", CS = "ⲓⲟⲩⲇ", ARFull = "يهوذيت" },
        new BibleBookInfo { BookNumber = 20, EN = "Est", AR = "اس", CB = "ⲉⲥⲑ", CS = "ⲉⲥⲑ", ARFull = "أستير" },
        new BibleBookInfo { BookNumber = 21, EN = "Job", AR = "اي", CB = "ⲓⲱⲃ", CS = "ⲓⲱⲃ", ARFull = "أيوب" },
        new BibleBookInfo { BookNumber = 22, EN = "Ps", AR = "مز", CB = "ⲯⲁⲗ", CS = "ⲯⲁⲗ", ARFull = "المزامير" },
        new BibleBookInfo { BookNumber = 23, EN = "Prov", AR = "ام", CB = "ⲡⲁⲣ", CS = "ⲡⲁⲣ", ARFull = "الأمثال" },
        new BibleBookInfo { BookNumber = 24, EN = "Ecc", AR = "جا", CB = "ⲉⲕⲕⲗ", CS = "ⲉⲕⲕⲗ", ARFull = "الجامعة" },
        new BibleBookInfo { BookNumber = 25, EN = "Song", AR = "نش", CB = "ⲁⲓⲥⲙ", CS = "ⲁⲓⲥⲙ", ARFull = "نشيد الأنشاد" },
        new BibleBookInfo { BookNumber = 26, EN = "Wis", AR = "حك", CB = "ⲥⲟⲫ", CS = "ⲥⲟⲫ", ARFull = "الحكمة" },
        new BibleBookInfo { BookNumber = 27, EN = "Sir", AR = "سير", CB = "ⲥⲉⲓⲣ", CS = "ⲥⲉⲓⲣ", ARFull = "يشوع بن سيراخ" },
        new BibleBookInfo { BookNumber = 28, EN = "Isa", AR = "اش", CB = "ⲏⲥ", CS = "ⲏⲥ", ARFull = "إشعياء" },
        new BibleBookInfo { BookNumber = 29, EN = "Jer", AR = "ار", CB = "ⲓⲉⲣ", CS = "ⲓⲉⲣ", ARFull = "إرميا" },
        new BibleBookInfo { BookNumber = 30, EN = "Lam", AR = "مرا", CB = "ⲑⲣ", CS = "ⲑⲣ", ARFull = "مراثي إرميا" },
        new BibleBookInfo { BookNumber = 31, EN = "Bar", AR = "بار", CB = "ⲃⲁ", CS = "ⲃⲁ", ARFull = "باروخ" },
        new BibleBookInfo { BookNumber = 32, EN = "Ezek", AR = "حز", CB = "ⲓⲉⲍ", CS = "ⲓⲉⲍ", ARFull = "حزقيال" },
        new BibleBookInfo { BookNumber = 33, EN = "Dan", AR = "دا", CB = "ⲇⲁⲛ", CS = "ⲇⲁⲛ", ARFull = "دانيال" },
        new BibleBookInfo { BookNumber = 34, EN = "Hos", AR = "هو", CB = "ϩⲱⲥ", CS = "ϩⲱⲥ", ARFull = "هوشع" },
        new BibleBookInfo { BookNumber = 35, EN = "Joel", AR = "يؤ", CB = "ⲓⲱⲗ", CS = "ⲓⲱⲗ", ARFull = "يوئيل" },
        new BibleBookInfo { BookNumber = 36, EN = "Amos", AR = "عا", CB = "ⲁⲙ", CS = "ⲁⲙ", ARFull = "عاموس" },
        new BibleBookInfo { BookNumber = 37, EN = "Obad", AR = "عو", CB = "ⲟⲃⲇ", CS = "ⲟⲃⲇ", ARFull = "عوبديا" },
        new BibleBookInfo { BookNumber = 38, EN = "Jonah", AR = "يون", CB = "ⲓⲱⲛ", CS = "ⲓⲱⲛ", ARFull = "يونان" },
        new BibleBookInfo { BookNumber = 39, EN = "Mic", AR = "مي", CB = "ⲙⲓⲭ", CS = "ⲙⲓⲭ", ARFull = "ميخا" },
        new BibleBookInfo { BookNumber = 40, EN = "Nah", AR = "نا", CB = "ⲛⲁ", CS = "ⲛⲁ", ARFull = "ناحوم" },
        new BibleBookInfo { BookNumber = 41, EN = "Hab", AR = "حب", CB = "ⲁⲙⲃ", CS = "ⲁⲙⲃ", ARFull = "حبقوق" },
        new BibleBookInfo { BookNumber = 42, EN = "Zeph", AR = "صف", CB = "ⲥⲁⲫ", CS = "ⲥⲁⲫ", ARFull = "صفنيا" },
        new BibleBookInfo { BookNumber = 43, EN = "Hag", AR = "حج", CB = "ϩⲅ", CS = "ϩⲅ", ARFull = "حجي" },
        new BibleBookInfo { BookNumber = 44, EN = "Zech", AR = "زك", CB = "ⲍⲁⲭ", CS = "ⲍⲁⲭ", ARFull = "زكريا" },
        new BibleBookInfo { BookNumber = 45, EN = "Mal", AR = "ملا", CB = "ⲙⲁⲗⲁⲭ", CS = "ⲙⲁⲗⲁⲭ", ARFull = "ملاخي" },
        new BibleBookInfo { BookNumber = 46, EN = "1Macc", AR = "1مك", CB = "ⲁ̅ ⲙⲁⲕ", CS = "ⲁ̅ ⲙⲁⲕ", ARFull = "مكابيين أول" },
        new BibleBookInfo { BookNumber = 47, EN = "2Macc", AR = "2مك", CB = "ⲃ̅ ⲙⲁⲕ", CS = "ⲃ̅ ⲙⲁⲕ", ARFull = "مكابيين ثاني" },
        new BibleBookInfo { BookNumber = 48, EN = "Mat", AR = "مت", CB = "ⲘⲀⲦ", CS = "ⲘⲀⲦ", ARFull = "متى" },
        new BibleBookInfo { BookNumber = 49, EN = "Mar", AR = "مر", CB = "ⳘⲀⲢ", CS = "ⳘⲀⲢ", ARFull = "مرقس" },
        new BibleBookInfo { BookNumber = 50, EN = "Luk", AR = "لو", CB = "ⲖⲞⲨ", CS = "ⲖⲞⲨ", ARFull = "لوقا" },
        new BibleBookInfo { BookNumber = 51, EN = "Joh", AR = "يو", CB = "ⲒⲰⲀ", CS = "ⲒⲰⲀ", ARFull = "يوحنا" },
        new BibleBookInfo { BookNumber = 52, EN = "Act", AR = "اع", CB = "ⲠⲢⲀ", CS = "ⲠⲢⲀ", ARFull = "أعمال الرسل" },
        new BibleBookInfo { BookNumber = 53, EN = "Rom", AR = "رو", CB = "ⲢⲰⳘ", CS = "ⲢⲰⳘ", ARFull = "رومية" },
        new BibleBookInfo { BookNumber = 54, EN = "1Co", AR = "1كو", CB = "ⲁ̅ ⲔⲞⲢ", CS = "ⲁ̅ ⲔⲞⲢ", ARFull = "كورنثوس الأولى" },
        new BibleBookInfo { BookNumber = 55, EN = "2Co", AR = "2كو", CB = "ⲃ̅̅ ⲔⲞⲢ", CS = "ⲃ̅̅ ⲔⲞⲢ", ARFull = "كورنثوس الثانية" },
        new BibleBookInfo { BookNumber = 56, EN = "Gal", AR = "غل", CB = "ⲄⲀⲖ", CS = "ⲄⲀⲖ", ARFull = "غلاطية" },
        new BibleBookInfo { BookNumber = 57, EN = "Eph", AR = "اف", CB = "ⲈⲪⲈ", CS = "ⲈⲪⲈ", ARFull = "أفسس" },
        new BibleBookInfo { BookNumber = 58, EN = "Phi", AR = "في", CB = "ⲪⲒⲖ", CS = "ⲪⲒⲖ", ARFull = "فيلبي" },
        new BibleBookInfo { BookNumber = 59, EN = "Col", AR = "كو", CB = "ⲔⲞⲖ", CS = "ⲔⲞⲖ", ARFull = "كولوسي" },
        new BibleBookInfo { BookNumber = 60, EN = "1Th", AR = "1تس", CB = "ⲁ̅ ⲐⲈⲤ", CS = "ⲁ̅ ⲐⲈⲤ", ARFull = "تسالونيكي الأولى" },
        new BibleBookInfo { BookNumber = 61, EN = "2Th", AR = "2تس", CB = "ⲃ̅ ⲐⲈⲤ", CS = "ⲃ̅ ⲐⲈⲤ", ARFull = "تسالونيكي الثانية" },
        new BibleBookInfo { BookNumber = 62, EN = "1Ti", AR = "1تي", CB = "ⲁ̅ ⲦⲒⳘ", CS = "ⲁ̅ ⲦⲒⳘ", ARFull = "تيموثاوس الأولى" },
        new BibleBookInfo { BookNumber = 63, EN = "2Ti", AR = "2تي", CB = "ⲃ̅ ⲦⲒⳘ", CS = "ⲃ̅ ⲦⲒⳘ", ARFull = "تيموثاوس الثانية" },
        new BibleBookInfo { BookNumber = 64, EN = "Tit", AR = "تي", CB = "ⲦⲒⲦ", CS = "ⲦⲒⲦ", ARFull = "تيطس" },
        new BibleBookInfo { BookNumber = 65, EN = "Phm", AR = "فل", CB = "ⲪⲒⲘ", CS = "ⲪⲒⲘ", ARFull = "فليمون" },
        new BibleBookInfo { BookNumber = 66, EN = "Heb", AR = "عب", CB = "ϩⲉⲃ", CS = "ϩⲉⲃ", ARFull = "عبرانيين" },
        new BibleBookInfo { BookNumber = 67, EN = "Jas", AR = "يع", CB = "ⲒⲀⲔ", CS = "ⲒⲀⲔ", ARFull = "يعقوب" },
        new BibleBookInfo { BookNumber = 68, EN = "1Pe", AR = "1بط", CB = "ⲁ̅ ⲠⲈⲦ", CS = "ⲁ̅ ⲠⲈⲦ", ARFull = "بطرس الأولى" },
        new BibleBookInfo { BookNumber = 69, EN = "2Pe", AR = "2بط", CB = "ⲃ̅ ⲠⲈⲦ", CS = "ⲃ̅ ⲠⲈⲦ", ARFull = "بطرس الثانية" },
        new BibleBookInfo { BookNumber = 70, EN = "1Jn", AR = "1يو", CB = "ⲁ̅ ⲒⲰⲀ", CS = "ⲁ̅ ⲒⲰⲀ", ARFull = "يوحنا الأولى" },
        new BibleBookInfo { BookNumber = 71, EN = "2Jn", AR = "2يو", CB = "ⲃ̅ ⲒⲰⲀ", CS = "ⲃ̅ ⲒⲰⲀ", ARFull = "يوحنا الثانية" },
        new BibleBookInfo { BookNumber = 72, EN = "3Jn", AR = "3يو", CB = "ⲅ̅ ⲒⲰⲀ", CS = "ⲅ̅ ⲒⲰⲀ", ARFull = "يوحنا الثالثة" },
        new BibleBookInfo { BookNumber = 73, EN = "Jud", AR = "يه", CB = "ⲒⲞⲨ", CS = "ⲒⲞⲨ", ARFull = "يهوذا" },
        new BibleBookInfo { BookNumber = 74, EN = "Rev", AR = "رؤ", CB = "ⲀⲠⲞ", CS = "ⲀⲠⲞ", ARFull = "رؤيا يوحنا" }
    };
        }



        // Enhanced method to get book number by abbreviation, short name, or full name in any language
        private int GetBibleBookNumber(string searchTerm, string language = "")
        {
            var books = GetBibleBooksList();
            var normalizedSearch = searchTerm?.Trim().ToLower() ?? "";

            // If language is specified, search in that specific language
            if (!string.IsNullOrEmpty(language))
            {
                var book = language.ToUpper() switch
                {
                    "EN" => books.FirstOrDefault(b =>
                        b.EN.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)),
                    "AR" => books.FirstOrDefault(b =>
                        b.AR == searchTerm || b.ARFull == searchTerm),
                    "C-B" => books.FirstOrDefault(b => b.CB == searchTerm),
                    "C-S" => books.FirstOrDefault(b => b.CS == searchTerm),
                    _ => null
                };
                return book?.BookNumber ?? 0;
            }

            // Search across all languages if no specific language is provided
            var foundBook = books.FirstOrDefault(b =>
                b.EN.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                b.AR == searchTerm ||
                b.ARFull == searchTerm ||
                b.CB == searchTerm ||
                b.CS == searchTerm ||
                b.ARFull.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) // Partial match for full Arabic names
            );

            return foundBook?.BookNumber ?? 0;
        }

        // Enhanced method to get SelectList for Bible books with full names for better display
        private SelectList GetBibleBooksSelectList(string language = "EN", bool includeFullNames = false)
        {
            var books = GetBibleBooksList();
            var items = books.Select(book => new SelectListItem
            {
                Value = book.BookNumber.ToString(),
                Text = language.ToUpper() switch
                {
                    "EN" => $"{book.BookNumber}. {book.EN}",
                    "AR" => includeFullNames ?
                        $"{book.BookNumber}. {book.ARFull} ({book.AR})" :
                        $"{book.BookNumber}. {book.AR}",
                    "C-B" => $"{book.BookNumber}. {book.CB}",
                    "C-S" => $"{book.BookNumber}. {book.CS}",
                    _ => $"{book.BookNumber}. {book.EN}"
                }
            }).ToList();

            return new SelectList(items, "Value", "Text");
        }

        // Method to search books by name (useful for autocomplete or search functionality)
        private List<BibleBookInfo> SearchBibleBooks(string searchTerm, string language = "")
        {
            var books = GetBibleBooksList();
            var normalizedSearch = searchTerm?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(normalizedSearch))
                return books;

            return books.Where(book =>
            {
                if (!string.IsNullOrEmpty(language))
                {
                    return language.ToUpper() switch
                    {
                        "EN" => book.EN.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase),
                        "AR" => book.AR.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                               book.ARFull.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase),
                        "C-B" => book.CB.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase),
                        "C-S" => book.CS.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase),
                        _ => false
                    };
                }

                // Search across all languages
                return book.EN.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                       book.AR.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                       book.ARFull.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                       book.CB.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                       book.CS.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        // Method to get book display name with option to show full Arabic name
        private string GetBibleBookDisplayName(int bookNumber, string language = "EN", bool useFullName = false)
        {
            var book = GetBibleBooksList().FirstOrDefault(b => b.BookNumber == bookNumber);
            if (book == null) return $"Book {bookNumber}";

            return language.ToUpper() switch
            {
                "EN" => $"{bookNumber}. {book.EN}",
                "AR" => useFullName ?
                    $"{bookNumber}. {book.ARFull}" :
                    $"{bookNumber}. {book.AR}",
                "C-B" => $"{bookNumber}. {book.CB}",
                "C-S" => $"{bookNumber}. {book.CS}",
                _ => $"{bookNumber}. {book.EN}"
            };
        }

        // API endpoint to get available editions based on book and language
        [HttpGet]
        public async Task<JsonResult> GetAvailableEditions(int? bookNumber, string language)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                // Filter by book if provided
                if (bookNumber.HasValue && bookNumber.Value > 0)
                {
                    query = query.Where(b => b.Book == bookNumber.Value);
                }

                // Filter by language if provided
                if (!string.IsNullOrEmpty(language))
                {
                    query = query.Where(b => b.Language == language);
                }

                // Get distinct editions
                var editions = await query
                    .Select(b => b.Edition)
                    .Distinct()
                    .Where(e => !string.IsNullOrEmpty(e))
                    .OrderBy(e => e)
                    .ToListAsync();

                return Json(new { success = true, editions = editions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint to get available chapters for a specific book, language, and edition
        [HttpGet]
        public async Task<JsonResult> GetAvailableChapters(int bookNumber, string language, string edition)
        {
            try
            {
                var chapters = await _context.Bibles
                    .Where(b => b.Book == bookNumber
                               && b.Language == language
                               && b.Edition == edition)
                    .Select(b => b.Chapter)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Json(new { success = true, chapters = chapters });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint to get available verses for a specific book, chapter, language, and edition
        [HttpGet]
        public async Task<JsonResult> GetAvailableVerses(int bookNumber, int chapter, string language, string edition)
        {
            try
            {
                var verses = await _context.Bibles
                    .Where(b => b.Book == bookNumber
                               && b.Chapter == chapter
                               && b.Language == language
                               && b.Edition == edition)
                    .Select(b => b.Verse)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToListAsync();

                return Json(new { success = true, verses = verses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


    }


}









#region

//public async Task<IActionResult> Edit(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        return NotFound();
//    }

//    // Fetch groups and roots for the initial page load
//    ViewData["GroupID"] = new SelectList(_context.Groups
//        .Select(g => new {
//            g.ID,
//            DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//        }), "ID", "DisplayField");

//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    return View(word);
//}

//// POST: Words/Edit/5
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (id != word.WordId)
//    {
//        return NotFound();
//    }

//    if (ModelState.IsValid)
//    {
//        try
//        {
//            _context.Update(word);
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!WordExists(word.WordId))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }
//        return RedirectToAction(nameof(Index));
//    }

//    // Repopulate dropdowns if the model state is invalid
//    ViewData["GroupID"] = new SelectList(_context.Groups
//        .Select(g => new {
//            g.ID,
//            DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//        }), "ID", "DisplayField", word.GroupID);

//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//    return View(word);
//}

//// GET: Words/FilterRoots
//[HttpGet]
//public async Task<IActionResult> FilterRoots(string searchTerm)
//{
//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        // Normalize the search term and apply the filter
//        string normalizedSearch = NormalizeString(searchTerm.Trim().ToLower());
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var filteredRoots = await rootWordsQuery
//        .Where(w => w.Language.StartsWith("C-")) // Additional filter for roots
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        })
//        .ToListAsync();

//    return Json(filteredRoots);
//}








//// GET: Words/Edit/5
//public async Task<IActionResult> Edit(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        return NotFound();
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new
//    {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField");

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-") && w.RootID == null)
//        .Select(w => new
//        {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    return View(word);
//}

//// POST: Words/Edit/5
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (id != word.WordId)
//    {
//        return NotFound();
//    }

//    if (ModelState.IsValid)
//    {
//        try
//        {
//            _context.Update(word);
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!WordExists(word.WordId))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }
//        return RedirectToAction(nameof(Index));
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new
//    {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField", word.GroupID);

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new
//        {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//    return View(word);
//}

// GET: Words/Edit/5





//[HttpGet]
//public IActionResult SearchRoots(string searchTerm)
//{
//    _logger.LogInformation("SearchRoots called with searchTerm: {SearchTerm}", searchTerm);

//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var roots = rootWordsQuery.Select(w => new { w.WordId, w.Word_text }).ToList();
//    return Json(roots);
//}

//[HttpGet]
//public IActionResult SearchGroups(string searchTerm)
//{
//    _logger.LogInformation("SearchGroups called with searchTerm: {SearchTerm}", searchTerm);

//    var groupsQuery = _context.Words
//        .Include(w => w.GroupWord)
//        .AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        groupsQuery = groupsQuery.Where(w => w.GroupWord != null && w.GroupWord.Name.Contains(normalizedSearch));
//    }

//    var groupResults = groupsQuery
//        .Select(w => new { w.GroupID, GroupName = w.GroupWord.Name })
//        .Distinct()
//        .ToList();

//    return Json(groupResults);
//}
//[HttpGet]
//public IActionResult SearchRoots(string searchTerm)
//{
//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var roots = rootWordsQuery.Select(w => new { w.WordId, w.Word_text }).ToList();
//    return Json(roots);
//}

//[HttpGet]
//public IActionResult SearchGroups(string searchTerm)
//{
//    var groupsQuery = _context.Words
//        .Include(w => w.GroupWord)
//        .AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        groupsQuery = groupsQuery.Where(w => w.GroupWord != null && w.GroupWord.Name.Contains(normalizedSearch));
//    }

//    var groupResults = groupsQuery
//        .Select(w => new { w.GroupID, GroupName = w.GroupWord.Name })
//        .Distinct()
//        .ToList();

//    return Json(groupResults);
//}
//[HttpGet]
//public async Task<IActionResult> Edit(int id)
//{
//    _logger.LogInformation("Edit action started for WordId: {WordId}", id);

//    // Fetch the word to edit
//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        _logger.LogWarning("Word with ID {WordId} not found.", id);
//        return NotFound();
//    }

//    // Populate static dropdowns like languages
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

//    // Log existing root and group values
//    ViewData["RootSearch"] = word.RootID.HasValue
//        ? await _context.Words.Where(w => w.WordId == word.RootID)
//            .Select(w => w.Word_text)
//            .FirstOrDefaultAsync()
//        : string.Empty;

//    ViewData["GroupSearch"] = word.GroupID.HasValue
//        ? await _context.Groups.Where(g => g.ID == word.GroupID)
//            .Select(g => g.Name)
//            .FirstOrDefaultAsync()
//        : string.Empty;

//    _logger.LogInformation("Root and Group values populated for WordId: {WordId}", id);
//    return View(word);
//}

//[HttpGet]
//public async Task<IActionResult> AddDictionaryReference(int wordId)
//{
//    var word = await _context.Words.FindAsync(wordId);
//    if (word == null)
//    {
//        return NotFound();
//    }

//    var dictionaries = await _context.Dictionaries.ToListAsync();

//    var model = new DictionaryReferenceWordViewModel
//    {
//        WordID = wordId,
//        Dictionaries = dictionaries.Select(d => new SelectListItem
//        {
//            Value = d.ID.ToString(),
//            Text = d.DictionaryName
//        }).ToList()
//    };

//    return View(model);
//}

//[HttpPost]
//public async Task<IActionResult> AddDictionaryReference(DictionaryReferenceWordViewModel model, int wordId)
//{
//    if (ModelState.IsValid)
//    {
//        var dictionaryReference = new DictionaryReferenceWord
//        {
//            WordID = model.WordID,
//            DictionaryID = model.DictionaryID,
//            Reference = model.Reference,
//            Column = model.Column
//        };

//        _context.DictionaryReferenceWords.Add(dictionaryReference);
//        await _context.SaveChangesAsync();

//        return RedirectToAction("Details", new { id = model.WordID });
//    }

//    return View(model);
//}
// POST: Words/Create
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (ModelState.IsValid)
//    {
//        _context.Add(word);
//        await _context.SaveChangesAsync();
//        return RedirectToAction(nameof(Index));
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//    return View(word);
//}
// POST: Words/Create
// GET: Words/Create
//public IActionResult Create()
//{
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "Name","OriginLanguage", "EtymologyWord");
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Word_text", "Language" ,"Class");
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    return View();
//}



// GET: Words/Create
//public IActionResult Create()
//{
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID");
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class");
//    return View();
//}

//// POST: Words/Create
//// To protect from overposting attacks, enable the specific properties you want to bind to.
//// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (ModelState.IsValid)
//    {
//        _context.Add(word);
//        await _context.SaveChangesAsync();
//        return RedirectToAction(nameof(Index));
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//    return View(word);
//}

//// GET: Words/Edit/5
//public async Task<IActionResult> Edit(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        return NotFound();
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//    return View(word);
//}

//// POST: Words/Edit/5
//// To protect from overposting attacks, enable the specific properties you want to bind to.
//// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (id != word.WordId)
//    {
//        return NotFound();
//    }

//    if (ModelState.IsValid)
//    {
//        try
//        {
//            _context.Update(word);
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!WordExists(word.WordId))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }
//        return RedirectToAction(nameof(Index));
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//    ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//    return View(word);
//}













// GET: Words/Details/5
/// <summary>
/// 
/// 
/// 
/// 
/// 
/// details original
/// </summary>
/// <returns></returns>
//public async Task<IActionResult> Details(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words
//        .Include(w => w.GroupWord)
//        .Include(w => w.Root)
//        .FirstOrDefaultAsync(m => m.WordId == id);
//    if (word == null)
//    {
//        return NotFound();
//    }

//    return View(word);
//}




////2
//public async Task<IActionResult> Details(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.Words)
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.GroupExplanations)
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Meaning)
//                .ThenInclude(m => m.MeaningTranslations)
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Examples)
//                .ThenInclude(e => e.ExampleTranslations)
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.WordMeaningBibles)
//                .ThenInclude(wmb => wmb.Bible)
//        .Include(w => w.DictionaryReferenceWords)
//            .ThenInclude(drw => drw.Dictionary)
//        .Include(w => w.DrevWords)
//            .ThenInclude(dw => dw.Word2) // The related words
//        .Include(w => w.Root)
//        .FirstOrDefaultAsync(m => m.WordId == id);

//    if (word == null)
//    {
//        return NotFound();
//    }

//    return View(word);
//}

////3333333333
//public async Task<IActionResult> Details(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.Words) // Include words in the GroupWord
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.GroupExplanations) // Include group explanations
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Meaning) // Include the meaning for the word
//                .ThenInclude(m => m.MeaningTranslations) // Include meaning translations
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Examples) // Include examples for word meanings
//                .ThenInclude(e => e.ExampleTranslations) // Include example translations
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.WordMeaningBibles) // Include Bible references for word meanings
//                .ThenInclude(wmb => wmb.Bible) // Include Bible details
//        .Include(w => w.DictionaryReferenceWords)
//            .ThenInclude(drw => drw.Dictionary) // Include dictionary references
//        .Include(w => w.DrevWords)
//            .ThenInclude(dw => dw.Word2) // Include derived words
//        .Include(w => w.Root) // Include the root word
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Meaning) // Navigate to the meaning
//                .ThenInclude(m => m.WordMeanings) // Include related WordMeanings
//                .ThenInclude(wm => wm.Word) // Include the Word related to the Meaning
//        .FirstOrDefaultAsync(m => m.WordId == id); // Fetch the word by its ID

//    if (word == null)
//    {
//        return NotFound();
//    }

//    return View(word);
//}





//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//public class WordsController : Controller
//{
//    private readonly ApplicationDbContext _context;

//    public WordsController(ApplicationDbContext context)
//    {
//        _context = context;
//    }

//    private List<SelectListItem> GetLanguagesList()
//    {
//        return new List<SelectListItem>
//        {
//            new SelectListItem { Value = "AR", Text = "Arabic" },
//            new SelectListItem { Value = "FR", Text = "French" },
//            new SelectListItem { Value = "EN", Text = "English" },
//            new SelectListItem { Value = "RU", Text = "Russian" },
//            new SelectListItem { Value = "DE", Text = "German" },
//            new SelectListItem { Value = "IT", Text = "Italian" },
//            new SelectListItem { Value = "HE", Text = "Hebrew" },
//            new SelectListItem { Value = "GR", Text = "Greek" },
//            new SelectListItem { Value = "ARC", Text = "Aramaic" },
//            new SelectListItem { Value = "EG",  Text = "Egyptian" },
//            new SelectListItem { Value = "C-B" , Text = "Coptic - B" },
//            new SelectListItem { Value = "C-S",  Text = "Coptic - S" },
//            new SelectListItem { Value = "C-Sa", Text = "Coptic - Sa" },
//            new SelectListItem { Value = "C-Sf", Text = "Coptic - Sf" },
//            new SelectListItem { Value = "C-A",  Text = "Coptic - A" },
//            new SelectListItem { Value = "C-sA", Text = "Coptic - sA" },
//            new SelectListItem { Value = "C-F",  Text = "Coptic - F" },
//            new SelectListItem { Value = "C-Fb", Text = "Coptic - Fb" },
//            new SelectListItem { Value = "C-O",  Text = "Coptic - O" },
//            new SelectListItem { Value = "C-NH", Text = "Coptic - NH" }
//        };
//    }

//    // GET: Words/Create
//    public IActionResult Create()
//    {
//        ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID");
//        ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class");
//        ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//        return View();
//    }

//    // POST: Words/Create
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//    {
//        if (ModelState.IsValid)
//        {
//            _context.Add(word);
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }
//        ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//        ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//        ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//        return View(word);
//    }

//    // GET: Words/Edit/5
//    public async Task<IActionResult> Edit(int? id)
//    {
//        if (id == null)
//        {
//            return NotFound();
//        }

//        var word = await _context.Words.FindAsync(id);
//        if (word == null)
//        {
//            return NotFound();
//        }
//        ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//        ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//        ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//        return View(word);
//    }

//    // POST: Words/Edit/5
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//    {
//        if (id != word.WordId)
//        {
//            return NotFound();
//        }

//        if (ModelState.IsValid)
//        {
//            try
//            {
//                _context.Update(word);
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!WordExists(word.WordId))
//                {
//                    return NotFound();
//                }
//                else
//                {
//                    throw;
//                }
//            }
//            return RedirectToAction(nameof(Index));
//        }
//        ViewData["GroupID"] = new SelectList(_context.Groups, "ID", "ID", word.GroupID);
//        ViewData["RootID"] = new SelectList(_context.Words, "WordId", "Class", word.RootID);
//        ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//        return View(word);
//    }

//    private bool WordExists(int id)
//    {
//        return _context.Words.Any(e => e.WordId == id);
//    }
//}









// POST: CreateMeaning (Handles form submission, saves meaning, and links it to the word)
//[HttpPost]
//[ValidateAntiForgeryToken]


//    public async Task<IActionResult> SelectFromMeaning (int wordId, int meaningID)
//    {
//        if (ModelState.IsValid)
//        {
//            // Create the WordMeaning relationship
//            WordMeaning wordMeaning = new WordMeaning
//            {
//                WordID = wordId,  // Link the selected word
//                MeaningID = meaningID // Link the selected meaning from the dropdown
//            };

//            _context.WordMeanings.Add(wordMeaning);
//            await _context.SaveChangesAsync();

//            // Redirect to the word details page
//            return RedirectToAction("Details", new { id = wordId });
//        }

//        // If form is invalid, repopulate the meanings dropdown
//        ViewBag.Meanings = new SelectList(_context.Meanings.ToList(), "ID", "MeaningText");
//        ViewBag.WordId = wordId;
//        return View();
//    }


// GET: Words/ConfirmDeleteWordMeaning/5
//public async Task<IActionResult> DeleteWordMeaning(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var wordMeaning = await _context.WordMeanings
//        .Include(w => w.Meaning)
//        .Include(w => w.Word)
//        .FirstOrDefaultAsync(m => m.ID == id);
//    if (wordMeaning == null)
//    {
//        return NotFound();
//    }

//    return View(wordMeaning);
//}

//// POST: WordMeanings/Delete/5
//[HttpPost, ActionName("DeleteWordMeaning")]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> DeleteConfirmedWordMeaning(int id)
//{
//    var wordMeaning = await _context.WordMeanings.FindAsync(id);
//    if (wordMeaning != null)
//    {
//        _context.WordMeanings.Remove(wordMeaning);
//    }

//    await _context.SaveChangesAsync();
//    return RedirectToAction(nameof(Index));
//}


// GET: WordMeanings/Delete/5
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> DeleteConfirmedWordMeaning(int id)
//{
//    var wordMeaning = await _context.WordMeanings.FindAsync(id);
//    if (wordMeaning != null)
//    {
//        _context.WordMeanings.Remove(wordMeaning);
//        await _context.SaveChangesAsync();
//    }

//    return RedirectToAction(nameof(Index)); // Redirect back to the list after deletion
//}


//public async Task<IActionResult> Edit(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        return NotFound();
//    }

//    // Fetch groups and roots for the initial page load
//    ViewData["GroupID"] = new SelectList(_context.Groups
//        .Select(g => new {
//            g.ID,
//            DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//        }), "ID", "DisplayField");

//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    return View(word);
//}

//// POST: Words/Edit/5
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (id != word.WordId)
//    {
//        return NotFound();
//    }

//    if (ModelState.IsValid)
//    {
//        try
//        {
//            _context.Update(word);
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!WordExists(word.WordId))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }
//        return RedirectToAction(nameof(Index));
//    }

//    // Repopulate dropdowns if the model state is invalid
//    ViewData["GroupID"] = new SelectList(_context.Groups
//        .Select(g => new {
//            g.ID,
//            DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//        }), "ID", "DisplayField", word.GroupID);

//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//    return View(word);
//}

//// GET: Words/FilterRoots
//[HttpGet]
//public async Task<IActionResult> FilterRoots(string searchTerm)
//{
//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        // Normalize the search term and apply the filter
//        string normalizedSearch = NormalizeString(searchTerm.Trim().ToLower());
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var filteredRoots = await rootWordsQuery
//        .Where(w => w.Language.StartsWith("C-")) // Additional filter for roots
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        })
//        .ToListAsync();

//    return Json(filteredRoots);
//}








//// GET: Words/Edit/5
//public async Task<IActionResult> Edit(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        return NotFound();
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new
//    {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField");

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-") && w.RootID == null)
//        .Select(w => new
//        {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    return View(word);
//}

//// POST: Words/Edit/5
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Edit(int id, [Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (id != word.WordId)
//    {
//        return NotFound();
//    }

//    if (ModelState.IsValid)
//    {
//        try
//        {
//            _context.Update(word);
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!WordExists(word.WordId))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }
//        return RedirectToAction(nameof(Index));
//    }
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new
//    {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField", word.GroupID);

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new
//        {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
//    return View(word);
//}

// GET: Words/Edit/5





//[HttpGet]
//public IActionResult SearchRoots(string searchTerm)
//{
//    _logger.LogInformation("SearchRoots called with searchTerm: {SearchTerm}", searchTerm);

//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var roots = rootWordsQuery.Select(w => new { w.WordId, w.Word_text }).ToList();
//    return Json(roots);
//}

//[HttpGet]
//public IActionResult SearchGroups(string searchTerm)
//{
//    _logger.LogInformation("SearchGroups called with searchTerm: {SearchTerm}", searchTerm);

//    var groupsQuery = _context.Words
//        .Include(w => w.GroupWord)
//        .AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        groupsQuery = groupsQuery.Where(w => w.GroupWord != null && w.GroupWord.Name.Contains(normalizedSearch));
//    }

//    var groupResults = groupsQuery
//        .Select(w => new { w.GroupID, GroupName = w.GroupWord.Name })
//        .Distinct()
//        .ToList();

//    return Json(groupResults);
//}
//[HttpGet]
//public IActionResult SearchRoots(string searchTerm)
//{
//    var rootWordsQuery = _context.Words.AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    var roots = rootWordsQuery.Select(w => new { w.WordId, w.Word_text }).ToList();
//    return Json(roots);
//}

//[HttpGet]
//public IActionResult SearchGroups(string searchTerm)
//{
//    var groupsQuery = _context.Words
//        .Include(w => w.GroupWord)
//        .AsQueryable();

//    if (!string.IsNullOrEmpty(searchTerm))
//    {
//        string normalizedSearch = searchTerm.Trim().ToLower();
//        groupsQuery = groupsQuery.Where(w => w.GroupWord != null && w.GroupWord.Name.Contains(normalizedSearch));
//    }

//    var groupResults = groupsQuery
//        .Select(w => new { w.GroupID, GroupName = w.GroupWord.Name })
//        .Distinct()
//        .ToList();

//    return Json(groupResults);
//}
//[HttpGet]
//public async Task<IActionResult> Edit(int id)
//{
//    _logger.LogInformation("Edit action started for WordId: {WordId}", id);

//    // Fetch the word to edit
//    var word = await _context.Words.FindAsync(id);
//    if (word == null)
//    {
//        _logger.LogWarning("Word with ID {WordId} not found.", id);
//        return NotFound();
//    }

//    // Populate static dropdowns like languages
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

//    // Log existing root and group values
//    ViewData["RootSearch"] = word.RootID.HasValue
//        ? await _context.Words.Where(w => w.WordId == word.RootID)
//            .Select(w => w.Word_text)
//            .FirstOrDefaultAsync()
//        : string.Empty;

//    ViewData["GroupSearch"] = word.GroupID.HasValue
//        ? await _context.Groups.Where(g => g.ID == word.GroupID)
//            .Select(g => g.Name)
//            .FirstOrDefaultAsync()
//        : string.Empty;

//    _logger.LogInformation("Root and Group values populated for WordId: {WordId}", id);
//    return View(word);
//}

//[HttpGet]
//public async Task<IActionResult> AddDictionaryReference(int wordId)
//{
//    var word = await _context.Words.FindAsync(wordId);
//    if (word == null)
//    {
//        return NotFound();
//    }

//    var dictionaries = await _context.Dictionaries.ToListAsync();

//    var model = new DictionaryReferenceWordViewModel
//    {
//        WordID = wordId,
//        Dictionaries = dictionaries.Select(d => new SelectListItem
//        {
//            Value = d.ID.ToString(),
//            Text = d.DictionaryName
//        }).ToList()
//    };

//    return View(model);
//}

//[HttpPost]
//public async Task<IActionResult> AddDictionaryReference(DictionaryReferenceWordViewModel model, int wordId)
//{
//    if (ModelState.IsValid)
//    {
//        var dictionaryReference = new DictionaryReferenceWord
//        {
//            WordID = model.WordID,
//            DictionaryID = model.DictionaryID,
//            Reference = model.Reference,
//            Column = model.Column
//        };

//        _context.DictionaryReferenceWords.Add(dictionaryReference);
//        await _context.SaveChangesAsync();

//        return RedirectToAction("Details", new { id = model.WordID });
//    }

//    return View(model);
//}

//[HttpPost]
//public async Task<IActionResult> CreateGroupAsChild(int parentGroupID, string newGroupName, string? originLanguage, string? etymologyWord, string? etymology, string? notes, bool isCompound)
//{
//    // Check if the parent group exists
//    var parentGroupExists = await _context.Groups.AnyAsync(g => g.ID == parentGroupID);
//    if (!parentGroupExists)
//    {
//        ModelState.AddModelError("parentGroupID", "The specified parent group does not exist.");
//        return View(); // Return the same view to show validation errors
//    }

//    // Create new group
//    var newGroup = new GroupWord
//    {
//        Name = newGroupName,
//        OriginLanguage = originLanguage,
//        EtymologyWord = etymologyWord,
//        Etymology = etymology,
//        Notes = notes
//    };

//    // Add the new group to the context and save changes
//    _context.Groups.Add(newGroup);
//    await _context.SaveChangesAsync(); // Save here to get newGroup.ID

//    // Now create the GroupRelation
//    var groupRelation = new GroupRelation
//    {
//        ParentGroupID = parentGroupID, // The ID of the existing parent group
//        RelatedGroupID = newGroup.ID, // The ID of the newly created group
//        IsCompound = isCompound
//    };

//    _context.GroupRelations.Add(groupRelation);
//    await _context.SaveChangesAsync();

//    return RedirectToAction("Index"); // Redirect as appropriate
//}

//public IActionResult CreateDerivedGroup(int WordID)
//{
//    TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField");

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
//    ViewBag.WordID = WordID; // Pass the MeaningId to the view
//    return View();
//}

//// POST: CreateWord (Handles form submission, saves the word, and links it to the meaning)
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> CreateDerivedGroup(int WordID, [Bind("Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,IsReviewed,RootID,GroupID")] Word word)

//{
//    if (ModelState.IsValid)
//    {
//        // First, add the word to the database
//        _context.Words.Add(word);
//        await _context.SaveChangesAsync();

//        // Ensure the Word has been successfully saved and has a valid ID
//        if (word.WordId > 0)
//        {
//            // Now, create the WordMeaning relationship and save it
//            DrevWord drevWord = new DrevWord
//            {
//                WordID = WordID,    // This is the correct Word ID
//                RelatedWordID = word.WordId    // This is the selected Meaning ID
//            };

//            _context.DrevWords.Add(drevWord);
//            await _context.SaveChangesAsync();

//            // Redirect back to the meaning details page
//            return RedirectToAction("Details", "Words", new { id = WordID });

//        }
//    }

//    ViewBag.WordID = WordID; // If form is invalid, pass MeaningId again
//    return View(word);
//}


//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (ModelState.IsValid)
//    {
//        _context.Add(word);
//        await _context.SaveChangesAsync();
//        return RedirectToAction(nameof(Index));
//    }

//    // GroupID - Combine Name, OriginLanguage, and EtymologyWord for the display field
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField", word.GroupID);

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

//    return View(word);
//}




//new update
//public IActionResult Create()
//{
//    // GroupID - Combine Name, OriginLanguage, and EtymologyWord for the display field
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField");

//    // RootID - Only words that start with "C-"
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField");

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

//    return View();
//}

//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word)
//{
//    if (ModelState.IsValid)
//    {
//        _context.Add(word);
//        await _context.SaveChangesAsync();
//        return RedirectToAction(nameof(Index));
//    }

//    // Populate GroupID dropdown
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField", word.GroupID);

//    // Initial population of RootID dropdown - modify filter as needed
//    ViewData["RootID"] = new SelectList(_context.Words
//        .Where(w => w.Language.StartsWith("C-"))
//        .Select(w => new {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        }), "WordId", "DisplayField", word.RootID);

//    // Populate languages
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

//    return View(word);
//}


//[HttpGet]
//public JsonResult SearchWords(string searchTerm)
//{
//    var words = _context.Words
//        .Where(w => w.Word_text.Contains(searchTerm)) // Remove the language filter for testing
//        .Select(w => new
//        {
//            w.WordId,
//            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//        })
//        .ToList();

//    return Json(words);
//}




//search = NormalizeString(search);

//// Store the search text and type in ViewBag
//ViewBag.SearchText = search;
//    ViewBag.SearchType = searchType;

//    // Query the Words table
//    var wordsQuery = _context.Words.AsQueryable();

//var wordsList = await wordsQuery.ToListAsync();
//    // Apply the search type
//    switch (searchType)
//    {
//        case "exact":
//            // Exact match
//            wordsList = wordsList.Where(w => NormalizeString(w.Word_text) == search).ToList();
//            break;



//[HttpGet]
//public IActionResult Create(string RootSearch = "")
//{
//    var word = new Word(); // Initialize an empty Word object

//    // Store the search term in ViewData to repopulate the input field
//    RootSearch = NormalizeString(RootSearch); // Normalize the input term first

//    ViewData["RootSearch"] = RootSearch;

//    // Populate GroupID dropdown
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField");

//    // Change rootWordsQuery to IEnumerable<Word>
//    IEnumerable<Word> rootWordsQuery = _context.Words.AsEnumerable();  // Use IEnumerable here

//    if (!string.IsNullOrEmpty(RootSearch))
//    {
//        // Apply normalization and filtering in memory
//        rootWordsQuery = rootWordsQuery.Where(w => NormalizeString(w.Word_text).Contains(RootSearch));
//    }

//    // Populate the root list for the dropdown
//    ViewData["RootID"] = new SelectList(rootWordsQuery.Select(w => new {
//        w.WordId,
//        DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//    }).ToList(), "WordId", "DisplayField");

//    // Populate languages for the dropdown
//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");

//    return View(word);
//}






//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> Create([Bind("WordId,Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID,GroupID")] Word word, string RootSearch = "")
//{
//    if (ModelState.IsValid)
//    {
//        _context.Add(word);
//        await _context.SaveChangesAsync();
//        return RedirectToAction(nameof(Index));
//    }

//    // Repopulate dropdowns in case of validation error
//    ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
//        g.ID,
//        DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
//    }), "ID", "DisplayField", word.GroupID);

//    var rootWordsQuery = _context.Words.AsQueryable();
//    if (!string.IsNullOrEmpty(RootSearch))
//    {
//        string normalizedSearch = RootSearch.Trim().ToLower();
//        rootWordsQuery = rootWordsQuery.Where(w => EF.Functions.Like(w.Word_text.ToLower(), $"%{normalizedSearch}%"));
//    }

//    ViewData["RootID"] = new SelectList(rootWordsQuery.Select(w => new {
//        w.WordId,
//        DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
//    }).ToList(), "WordId", "DisplayField", word.RootID);

//    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);

//    return View(word); // Return the view with the word and populated dropdowns
//}

//public async Task<IActionResult> Details(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    // Fetch the word with all necessary related data
//    var word = await _context.Words
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.Words) // Include words in the GroupWord
//        .Include(w => w.GroupWord)
//            .ThenInclude(g => g.GroupExplanations) // Include group explanations
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Meaning) // Include the meaning for the word
//            .ThenInclude(m => m.ChildMeanings)
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.Examples) // Include examples for word meanings
//        .Include(w => w.WordMeanings)
//            .ThenInclude(wm => wm.WordMeaningBibles) // Include Bible references for word meanings
//                .ThenInclude(wmb => wmb.Bible) // Include Bible details
//        .Include(w => w.DictionaryReferenceWords)
//            .ThenInclude(drw => drw.Dictionary) // Include dictionary references
//        .Include(w => w.DrevWords)
//            .ThenInclude(dw => dw.Word2) // Include related Drev words
//        .Include(w => w.Root) // Include the root word
//        .Include(w => w.WordMeanings)
//                .ThenInclude(wm => wm.Meaning) // Navigate to the meaning
//                    .ThenInclude(m => m.WordMeanings) // Include related WordMeanings
//                    .ThenInclude(wm => wm.Word) // Include the Word related to the Meaning
//        .AsSplitQuery()
//        .FirstOrDefaultAsync(m => m.WordId == id); // Fetch the word by its ID

//    if (word == null)
//    {
//        return NotFound();
//    }

//    // Create a view model or data structure to store Bible verses with the same Book, Chapter, Verse
//    foreach (var wordMeaning in word.WordMeanings)
//    {
//        foreach (var wordMeaningBible in wordMeaning.WordMeaningBibles)
//        {
//            var bible = wordMeaningBible.Bible;

//            // Fetch other Bible verses with the same Book, Chapter, and Verse but different Language or Edition
//            var relatedBibleVerses = await _context.Bibles
//                .Where(b => b.Book == bible.Book && b.Chapter == bible.Chapter && b.Verse == bible.Verse
//                            && (b.Language != bible.Language || b.Edition != bible.Edition))
//                .ToListAsync();

//            // Store these related Bible verses in the ViewBag or create a ViewModel to pass this data to the view
//            ViewBag.RelatedBibleVerses ??= new Dictionary<int, List<Bible>>();
//            ViewBag.RelatedBibleVerses[wordMeaningBible.BibleID] = relatedBibleVerses;
//        }
//    }

//    return View(word);
//}
#endregion