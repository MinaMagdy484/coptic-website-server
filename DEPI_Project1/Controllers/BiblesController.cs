using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CopticDictionarynew1.Controllers
{
    [Authorize(Roles = "Student,Instructor,Admin")]
    public class BiblesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BiblesController(ApplicationDbContext context)
        {
            _context = context;
        }

       public async Task<IActionResult> Index(string? search, string? searchType = "contain", 
    int? bookNumber = null, int? chapter = null, string? language = null, string? edition = null, int? verse = null,
    int page = 1, int pageSize = 20) // Add pagination parameters
{
    // Store search parameters in ViewBag
    ViewBag.SearchText = search;
    ViewBag.SearchType = searchType;
    ViewBag.SelectedBook = bookNumber;
    ViewBag.SelectedChapter = chapter;
    ViewBag.SelectedLanguage = language;
    ViewBag.SelectedEdition = edition;
    ViewBag.SelectedVerse = verse;
    ViewBag.PageSize = pageSize;
    ViewBag.CurrentPage = page;

    // Get available filter options
    await PopulateFilterDropdowns(bookNumber, chapter, language, edition, verse);

    var biblesQuery = _context.Bibles.AsQueryable();

    // Apply filters
    if (bookNumber.HasValue)
    {
        biblesQuery = biblesQuery.Where(b => b.Book == bookNumber.Value);
    }

    if (chapter.HasValue)
    {
        biblesQuery = biblesQuery.Where(b => b.Chapter == chapter.Value);
    }

    if (!string.IsNullOrEmpty(language))
    {
        biblesQuery = biblesQuery.Where(b => b.Language == language);
    }

    if (!string.IsNullOrEmpty(edition))
    {
        biblesQuery = biblesQuery.Where(b => b.Edition == edition);
    }

    if (verse.HasValue)
    {
        biblesQuery = biblesQuery.Where(b => b.Verse == verse.Value);
    }

    // Apply text search if provided
    if (!string.IsNullOrEmpty(search))
    {
        var normalizedSearch = NormalizeString(search);
        var biblesList = await biblesQuery.ToListAsync();

        switch (searchType)
        {
            case "exact":
                biblesList = biblesList.Where(b => NormalizeString(b.Text) == normalizedSearch).ToList();
                break;
            case "contain":
                biblesList = biblesList.Where(b => NormalizeString(b.Text).Contains(normalizedSearch)).ToList();
                break;
            case "start":
                biblesList = biblesList.Where(b => NormalizeString(b.Text).StartsWith(normalizedSearch)).ToList();
                break;
            case "end":
                biblesList = biblesList.Where(b => NormalizeString(b.Text).EndsWith(normalizedSearch)).ToList();
                break;
            default:
                biblesList = biblesList.Where(b => NormalizeString(b.Text).Contains(normalizedSearch)).ToList();
                break;
        }

        var orderedBiblesList = biblesList.OrderBy(b => b.Book).ThenBy(b => b.Chapter).ThenBy(b => b.Verse).ToList();
        
        // Calculate pagination for search results
        var totalCount = orderedBiblesList.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var pagedResults = orderedBiblesList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.HasPreviousPage = page > 1;
        ViewBag.HasNextPage = page < totalPages;
        
        return View("Index", pagedResults);
    }
    else
    {
        // If no search text but filters are applied, return filtered results with pagination
        if (bookNumber.HasValue || chapter.HasValue || !string.IsNullOrEmpty(language) || !string.IsNullOrEmpty(edition))
        {
            var totalCount = await biblesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var pagedResults = await biblesQuery
                .OrderBy(b => b.Book)
                .ThenBy(b => b.Chapter)
                .ThenBy(b => b.Verse)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            
            return View("Index", pagedResults);
        }
        else
        {
            // No search and no filters - return empty list
            ViewBag.TotalPages = 0;
            ViewBag.TotalCount = 0;
            ViewBag.HasPreviousPage = false;
            ViewBag.HasNextPage = false;
            return View("Index", new List<Bible>());
        }
    }
}

        // Update the GetBookNameArabic method to return only the full Arabic name
        private string GetBookNameArabic(int bookNumber)
        {
            var books = GetBibleBooksList();
            var book = books.FirstOrDefault(b => b.BookNumber == bookNumber);
            
            if (book != null)
            {
                return book.ARFull; // Return only the full Arabic name
            }
            
            return $"كتاب {bookNumber}";
        }

        // Add a new method to get book name for dropdown (with number)
        private string GetBookNameForDropdown(int bookNumber)
        {
            var books = GetBibleBooksList();
            var book = books.FirstOrDefault(b => b.BookNumber == bookNumber);
            
            if (book != null)
            {
                return $"{book.BookNumber}. {book.ARFull} ({book.AR})";
            }
            
            return $"كتاب {bookNumber}";
        }

        // Update PopulateFilterDropdowns method
        private async Task PopulateFilterDropdowns(int? selectedBook = null, int? selectedChapter = null,
            string? selectedLanguage = null, string? selectedEdition = null, int? selectedVerse = null)
        {
            // Get available books from database
            var availableBooks = await _context.Bibles
                .Select(b => b.Book)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // Create book list with Arabic names using existing BibleBookInfo model
            var bookItems = availableBooks.Select(bookNum => new SelectListItem
            {
                Value = bookNum.ToString(),
                Text = GetBookNameForDropdown(bookNum) // Use the dropdown version with numbers
            }).ToList();

            ViewBag.BibleBooks = new SelectList(bookItems, "Value", "Text", selectedBook?.ToString());

            // Get available languages
            var availableLanguages = await _context.Bibles
                .Select(b => b.Language)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            ViewBag.Languages = new SelectList(availableLanguages.Select(l => new SelectListItem
            {
                Value = l,
                Text = GetLanguageDisplayName(l)
            }), "Value", "Text", selectedLanguage);

            // Get available editions (filtered by language if selected)
            var editionsQuery = _context.Bibles.AsQueryable();
            if (!string.IsNullOrEmpty(selectedLanguage))
            {
                editionsQuery = editionsQuery.Where(b => b.Language == selectedLanguage);
            }

            var availableEditions = await editionsQuery
                .Select(b => b.Edition)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            ViewBag.Editions = new SelectList(availableEditions, selectedEdition);

            // Get available chapters (filtered by book if selected)
            if (selectedBook.HasValue)
            {
                var availableChapters = await _context.Bibles
                    .Where(b => b.Book == selectedBook.Value)
                    .Select(b => b.Chapter)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                ViewBag.Chapters = new SelectList(availableChapters);
            }
            else
            {
                ViewBag.Chapters = new SelectList(new List<int>());
            }

            // Add verses dropdown if chapter is selected
            if (selectedBook.HasValue && selectedChapter.HasValue)
            {
                var versesQuery = _context.Bibles
                    .Where(b => b.Book == selectedBook.Value && b.Chapter == selectedChapter.Value);
                    
                if (!string.IsNullOrEmpty(selectedLanguage))
                {
                    versesQuery = versesQuery.Where(b => b.Language == selectedLanguage);
                }
                
                if (!string.IsNullOrEmpty(selectedEdition))
                {
                    versesQuery = versesQuery.Where(b => b.Edition == selectedEdition);
                }

                var availableVerses = await versesQuery
                    .Select(b => b.Verse)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToListAsync();

                ViewBag.Verses = new SelectList(availableVerses, selectedVerse);
            }
            else
            {
                ViewBag.Verses = new SelectList(new List<int>());
            }

            // Add book names dictionary to ViewBag for use in the view
            var bookNamesDictionary = availableBooks.ToDictionary(
                bookNum => bookNum,
                bookNum => GetBookNameArabic(bookNum)
            );
            ViewBag.BookNames = bookNamesDictionary;
        }

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

        private string GetLanguageDisplayName(string languageCode)
        {
            var languageNames = new Dictionary<string, string>
            {
                { "AR", "العربية (Arabic)" }, 
                { "FR", "الفرنسية (French)" }, 
                { "EN", "الإنجليزية (English)" }, 
                { "RU", "الروسية (Russian)" },
                { "DE", "الألمانية (German)" }, 
                { "IT", "الإيطالية (Italian)" }, 
                { "HE", "العبرية (Hebrew)" }, 
                { "GR", "اليونانية (Greek)" },
                { "ARC", "الآرامية (Aramaic)" }, 
                { "EG", "المصرية القديمة (Egyptian)" }, 
                { "C-B", "القبطية - البحيرية (Coptic - Bohairic)" },
                { "C-S", "القبطية - الصعيدية (Coptic - Sahidic)" }, 
                { "C-Sa", "القبطية - السخميمية (Coptic - Sakhmimic)" }, 
                { "C-Sf", "القبطية - الفيومية الفرعية (Coptic - Subfayyumic)" },
                { "C-A", "القبطية - الأخميمية (Coptic - Akhmimic)" }, 
                { "C-sA", "القبطية - الأخميمية الفرعية (Coptic - sub-Akhmimic)" }, 
                { "C-F", "القبطية - الفيومية (Coptic - Fayyumic)" },
                { "C-Fb", "القبطية - الفيومية ب (Coptic - Fayyumic B)" }, 
                { "C-O", "القبطية - الأوكسيرهينخية (Coptic - Oxyrhynchite)" }, 
                { "C-NH", "القبطية - نجع حمادي (Coptic - Nag Hammadi)" }
            };

            return languageNames.ContainsKey(languageCode) ? languageNames[languageCode] : languageCode;
        }

        // AJAX endpoints for dynamic filtering - Enhanced versions
        [HttpGet]
        public async Task<JsonResult> GetAvailableEditions(int? bookNumber = null, string? language = null)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                if (bookNumber.HasValue)
                {
                    query = query.Where(b => b.Book == bookNumber.Value);
                }

                if (!string.IsNullOrEmpty(language))
                {
                    query = query.Where(b => b.Language == language);
                }

                var editions = await query
                    .Select(b => b.Edition)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();

                return Json(new { success = true, editions = editions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableChapters(int? bookNumber = null, string? language = null, string? edition = null)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                if (bookNumber.HasValue)
                {
                    query = query.Where(b => b.Book == bookNumber.Value);
                }

                if (!string.IsNullOrEmpty(language))
                {
                    query = query.Where(b => b.Language == language);
                }

                if (!string.IsNullOrEmpty(edition))
                {
                    query = query.Where(b => b.Edition == edition);
                }

                var chapters = await query
                    .Select(b => b.Chapter)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Json(new { success = true, chapters = chapters });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableVerses(int? bookNumber = null, int? chapter = null, string? language = null, string? edition = null)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                if (bookNumber.HasValue)
                {
                    query = query.Where(b => b.Book == bookNumber.Value);
                }

                if (chapter.HasValue)
                {
                    query = query.Where(b => b.Chapter == chapter.Value);
                }

                if (!string.IsNullOrEmpty(language))
                {
                    query = query.Where(b => b.Language == language);
                }

                if (!string.IsNullOrEmpty(edition))
                {
                    query = query.Where(b => b.Edition == edition);
                }

                var verses = await query
                    .Select(b => b.Verse)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToListAsync();

                return Json(new { success = true, verses = verses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Enhanced method to get available books based on language and edition
        [HttpGet]
        public async Task<JsonResult> GetAvailableBooks(string? language = null, string? edition = null)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                if (!string.IsNullOrEmpty(language))
                {
                    query = query.Where(b => b.Language == language);
                }

                if (!string.IsNullOrEmpty(edition))
                {
                    query = query.Where(b => b.Edition == edition);
                }

                var bookNumbers = await query
                    .Select(b => b.Book)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                var books = bookNumbers.Select(bookNum => new
                {
                    value = bookNum,
                    text = GetBookNameForDropdown(bookNum),
                    arabicName = GetBookNameArabic(bookNum)
                }).ToList();

                return Json(new { success = true, books = books });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get available languages for a book
        [HttpGet]
        public async Task<JsonResult> GetAvailableLanguages(int? bookNumber = null)
        {
            try
            {
                var query = _context.Bibles.AsQueryable();

                if (bookNumber.HasValue)
                {
                    query = query.Where(b => b.Book == bookNumber.Value);
                }

                var languages = await query
                    .Select(b => b.Language)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

                var languagesList = languages.Select(lang => new 
                {
                    value = lang,
                    text = GetLanguageDisplayName(lang)
                }).ToList();

                return Json(new { success = true, languages = languagesList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.ToLowerInvariant();
            input = RemoveDiacritics(input);
            return input;
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

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

        // Standard CRUD operations remain the same...
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bible = await _context.Bibles
                .FirstOrDefaultAsync(m => m.BibleID == id);
    
            if (bible == null)
            {
                return NotFound();
            }

            // Get all related verses with the same book, chapter, and verse (including this one)
            var relatedVerses = await _context.Bibles
                .Where(b => b.Book == bible.Book && 
                           b.Chapter == bible.Chapter && 
                           b.Verse == bible.Verse)
                .OrderBy(b => b.Language)
                .ThenBy(b => b.Edition)
                .ToListAsync();

            // Get the book name for display
            string bookName = GetBookNameArabic(bible.Book);
            string bookNameEN = GetBibleBooksList().FirstOrDefault(b => b.BookNumber == bible.Book)?.EN ?? $"Book {bible.Book}";

            // Pass data to view using ViewBag
            ViewBag.BookName = bookName;
            ViewBag.BookNameEN = bookNameEN;
            ViewBag.RelatedVerses = relatedVerses;
            ViewBag.LanguageNames = GetLanguagesList().ToDictionary(l => l.Value, l => l.Text);

            return View(bible);
        }

        // Add this method to BiblesController class
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
                new SelectListItem { Value = "EG", Text = "Egyptian" },
                new SelectListItem { Value = "C-B", Text = "Coptic - B" },
                new SelectListItem { Value = "C-S", Text = "Coptic - S" },
                new SelectListItem { Value = "C-Sa", Text = "Coptic - Sa" },
                new SelectListItem { Value = "C-Sf", Text = "Coptic - Sf" },
                new SelectListItem { Value = "C-A", Text = "Coptic - A" },
                new SelectListItem { Value = "C-sA", Text = "Coptic - sA" },
                new SelectListItem { Value = "C-F", Text = "Coptic - F" },
                new SelectListItem { Value = "C-Fb", Text = "Coptic - Fb" },
                new SelectListItem { Value = "C-O", Text = "Coptic - O" },
                new SelectListItem { Value = "C-NH", Text = "Coptic - NH" }
            };
        }

        public IActionResult Create()
        {
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
            ViewData["BibleBooks"] = new SelectList(GetBibleBooksList().Select(b => new { Value = b.BookNumber, Text = $"{b.BookNumber}. {b.ARFull} ({b.AR})" }), "Value", "Text");
            return View();
        }

        // Update the Create POST action to redirect to Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BibleID,Book,Chapter,Verse,Language,Edition,Text,Pronunciation,Notes")] Bible bible)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bible);
                await _context.SaveChangesAsync();
                // Redirect to Details instead of Index
                return RedirectToAction("Details", new { id = bible.BibleID });
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", bible.Language);
            ViewData["BibleBooks"] = new SelectList(GetBibleBooksList().Select(b => new { Value = b.BookNumber, Text = $"{b.BookNumber}. {b.ARFull} ({b.AR})" }), "Value", "Text", bible.Book);
            return View(bible);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bible = await _context.Bibles.FindAsync(id);
            if (bible == null)
            {
                return NotFound();
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", bible.Language);
            ViewData["BibleBooks"] = new SelectList(GetBibleBooksList().Select(b => new { Value = b.BookNumber, Text = $"{b.BookNumber}. {b.ARFull} ({b.AR})" }), "Value", "Text", bible.Book);
            return View(bible);
        }

        // Update the Edit POST action to redirect to Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BibleID,Book,Chapter,Verse,Language,Edition,Text,Pronunciation,Notes")] Bible bible)
        {
            if (id != bible.BibleID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bible);
                    await _context.SaveChangesAsync();
                    // Redirect to Details instead of Index
                    return RedirectToAction("Details", new { id = bible.BibleID });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BibleExists(bible.BibleID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", bible.Language);
            ViewData["BibleBooks"] = new SelectList(GetBibleBooksList().Select(b => new { Value = b.BookNumber, Text = $"{b.BookNumber}. {b.ARFull} ({b.AR})" }), "Value", "Text", bible.Book);
            return View(bible);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bible = await _context.Bibles
                .FirstOrDefaultAsync(m => m.BibleID == id);
            if (bible == null)
            {
                return NotFound();
            }

            return View(bible);
        }

        

        private bool BibleExists(int id)
        {
            return _context.Bibles.Any(e => e.BibleID == id);
        }



        [HttpPost, ActionName("DeleteConfirmed")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var bible = await _context.Bibles.FindAsync(id);
    if (bible == null)
    {
        return NotFound();
    }
    
    // Store details for redirection after delete
    int book = bible.Book;
    int chapter = bible.Chapter;
    int verse = bible.Verse;
    
    _context.Bibles.Remove(bible);
    await _context.SaveChangesAsync();
    
    // Check if there are other versions of this verse
    var relatedVerse = await _context.Bibles
        .Where(b => b.Book == book && b.Chapter == chapter && b.Verse == verse)
        .FirstOrDefaultAsync();
    
    if (relatedVerse != null)
    {
        // If another version exists, redirect to its details page
        return RedirectToAction(nameof(Details), new { id = relatedVerse.BibleID });
    }
    
    // If no more versions exist, redirect to index
    return RedirectToAction(nameof(Index));
}
    }

    // Helper class for Bible Book Information
    public class BibleBookInfo
    {
        public int BookNumber { get; set; }
        public string EN { get; set; }
        public string AR { get; set; }
        public string CB { get; set; }
        public string CS { get; set; }
        public string ARFull { get; set; }
    }
}