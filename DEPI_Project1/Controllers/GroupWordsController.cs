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
    public class GroupWordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupWordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GroupWords
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.Groups.ToListAsync());
        //}


        public async Task<IActionResult> Index(string? search, string? groupSearchType = "contain", string? wordSearchType = "contain")
        {
            // Store the search parameters in ViewBag
            ViewBag.SearchText = search;
            ViewBag.GroupSearchType = groupSearchType;
            ViewBag.WordSearchType = wordSearchType;

            if (string.IsNullOrEmpty(search))
            {
                // Return empty list when no search is provided
                return View("Index", new List<GroupWord>());
            }

            // Normalize the search string
            search = NormalizeString(search);

            // Get all groups with their words
            var allGroupsWithWords = await _context.Groups
                .Include(g => g.Words)
                .ToListAsync();

            // Filter groups based on search criteria
            var filteredGroups = allGroupsWithWords
                .Where(g =>
                    // Search in group name based on groupSearchType
                    MatchGroupName(g.Name, search, groupSearchType) ||
                    // Search in words within the group based on wordSearchType
                    (g.Words != null && g.Words.Any(w => MatchWordText(w.Word_text, search, wordSearchType)))
                )
                .OrderBy(g => g.Name)
                .ToList();

            return View("Index", filteredGroups);
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


        // GET: GroupWords/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var groupWord = await _context.Groups
        //        .Include(m => m.Words)
        //        .Include(g => g.GroupExplanations)
        //        .Include(g => g.GroupParents) // Include parent group relations
        //                .ThenInclude(gr => gr.ParentGroup) // Include parent groups
        //                    .ThenInclude(m => m.Words)
        //        .Include(g => g.GroupChilds) // Include child group relations
        //                .ThenInclude(gr => gr.RelatedGroup) // Include child groups
        //                    .ThenInclude(m => m.Words)
        //                    .AsSplitQuery()
        //                    .FirstOrDefaultAsync(m => m.ID == id);
        //    if (groupWord == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(groupWord);
        //}        // GET: GroupWords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupWord = await _context.Groups
                .Include(g => g.Words) // Include words in the group
                    .ThenInclude(w => w.WordMeanings) // Include word meanings
                        .ThenInclude(wm => wm.Meaning) // Include meanings
                .Include(g => g.Words) // Include words again for other navigation properties
                    .ThenInclude(w => w.Root) // Include root words
                .Include(g => g.GroupExplanations) // Include group explanations
                .Include(g => g.GroupParents) // Include parent group relations
                    .ThenInclude(gp => gp.ParentGroup) // Include parent groups
                        .ThenInclude(pg => pg.Words) // Include words in parent groups
                .Include(g => g.GroupChilds) // Include child group relations
                    .ThenInclude(gc => gc.RelatedGroup) // Include child groups
                        .ThenInclude(cg => cg.Words) // Include words in child groups
                .Include(g => g.GroupExplanations)
                .AsSplitQuery()
                .FirstOrDefaultAsync(g => g.ID == id);

            if (groupWord == null)
            {
                return NotFound();
            }

            // Prepare grouped words data for display (similar to Words Details)
            if (groupWord.Words != null && groupWord.Words.Any())
            {
                var wordsInGroup = groupWord.Words.ToList();

                // Get unique classes (parts of speech) - Handle null values
                var uniqueClasses = wordsInGroup
                    .Select(w => w.Class ?? "Unknown") // Handle null Class values
                    .Distinct()
                    .OrderBy(c => c == "Unknown" ? "ZZZ" : c) // Sort "Unknown" to the end
                    .ToList();

                // Group words by language, then by class - Handle null values
                var groupedWords = wordsInGroup
                    .GroupBy(w => w.Language ?? "Unknown") // Handle null Language
                    .OrderBy(g => g.Key == "Unknown" ? "ZZZ" : g.Key) // Sort "Unknown" to the end
                    .ToDictionary(
                        languageGroup => languageGroup.Key,
                        languageGroup => languageGroup
                            .GroupBy(w => w.Class ?? "Unknown") // Handle null Class
                            .OrderBy(classGroup => classGroup.Key == "Unknown" ? "ZZZ" : classGroup.Key) // Sort "Unknown" to the end
                            .ToDictionary(
                                classGroup => classGroup.Key,
                                classGroup => classGroup.OrderBy(w => w.Word_text).ToList()
                            )
                    );

                ViewBag.GroupedWords = groupedWords;
                ViewBag.UniqueClasses = uniqueClasses;
            }

            return View(groupWord);
        }

        // public async Task<IActionResult> Details(int? id)
        // {
        //     if (id == null)
        //     {
        //         return NotFound();
        //     }

        //     var groupWord = await _context.Groups
        //         .Include(g => g.Words) // Include words in the group
        //             .ThenInclude(w => w.WordMeanings) // Include word meanings
        //                 .ThenInclude(wm => wm.Meaning) // Include meanings
        //         .Include(g => g.Words) // Include words again for other navigation properties
        //             .ThenInclude(w => w.Root) // Include root words
        //         .Include(g => g.GroupExplanations) // Include group explanations
        //         .Include(g => g.GroupParents) // Include parent group relations
        //             .ThenInclude(gp => gp.ParentGroup) // Include parent groups
        //                 .ThenInclude(pg => pg.Words) // Include words in parent groups
        //         .Include(g => g.GroupChilds) // Include child group relations
        //             .ThenInclude(gc => gc.RelatedGroup) // Include child groups
        //                 .ThenInclude(cg => cg.Words) // Include words in child groups
        //         .Include(g => g.GroupExplanations)
        //         .AsSplitQuery()
        //         .FirstOrDefaultAsync(g => g.ID == id);

        //     if (groupWord == null)
        //     {
        //         return NotFound();
        //     }

        //     // Prepare grouped words data for display (similar to Words Details)
        //     if (groupWord.Words != null && groupWord.Words.Any())
        //     {
        //         var wordsInGroup = groupWord.Words.ToList();

        //         // Get unique classes (parts of speech)
        //         var uniqueClasses = wordsInGroup
        //             .Where(w => !string.IsNullOrEmpty(w.Class))
        //             .Select(w => w.Class)
        //             .Distinct()
        //             .OrderBy(c => c)
        //             .ToList();

        //         // Group words by language, then by class
        //         var groupedWords = wordsInGroup
        //             .GroupBy(w => w.Language)
        //             .OrderBy(g => g.Key)
        //             .ToDictionary(
        //                 languageGroup => languageGroup.Key,
        //                 languageGroup => languageGroup
        //                     .GroupBy(w => w.Class ?? "Unspecified")
        //                     .ToDictionary(
        //                         classGroup => classGroup.Key,
        //                         classGroup => classGroup.OrderBy(w => w.Word_text).ToList()
        //                     )
        //             );

        //         ViewBag.GroupedWords = groupedWords;
        //         ViewBag.UniqueClasses = uniqueClasses;
        //     }

        //     return View(groupWord);
        // }



        // GET: GroupWords/Create
        public IActionResult Create()
        {
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        // POST: GroupWords/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,OriginLanguage,EtymologyWord,Etymology,Notes,Pronunciation")] GroupWord groupWord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(groupWord);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = groupWord.ID });
            }
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");
            return View(groupWord);
        }

        // GET: GroupWords/Edit/5
        // GET: GroupWords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupWord = await _context.Groups.FindAsync(id);
            if (groupWord == null)
            {
                return NotFound();
            }

            // Add language dropdown for Edit view
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text", groupWord.OriginLanguage);

            return View(groupWord);
        }
        // POST: GroupWords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: GroupWords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,OriginLanguage,EtymologyWord,Etymology,Notes,Pronunciation")] GroupWord groupWord)
        {
            if (id != groupWord.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(groupWord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupWordExists(groupWord.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // Redirect to Details instead of Index
                return RedirectToAction("Details", new { id = groupWord.ID });
            }
            return View(groupWord);
        }

        // GET: GroupWords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupWord = await _context.Groups
                .FirstOrDefaultAsync(m => m.ID == id);
            if (groupWord == null)
            {
                return NotFound();
            }

            return View(groupWord);
        }

        // POST: GroupWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var groupWord = await _context.Groups.FindAsync(id);
            if (groupWord != null)
            {
                _context.Groups.Remove(groupWord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupWordExists(int id)
        {
            return _context.Groups.Any(e => e.ID == id);
        }


        public IActionResult CreateGroupAsChild(int parentGroupID, int wordId)
        {
            var model = new CreateGroupViewModel
            {
                ParentGroupID = parentGroupID
            };
                ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

            ViewBag.WordId = wordId;
            return View(model);
        }

        // Action to handle form submission
        [HttpPost]
        public async Task<IActionResult> CreateGroupAsChild(CreateGroupViewModel model, int wordId)
        {
            if (!ModelState.IsValid)
            {
                    ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

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

            return RedirectToAction("Details", "GroupWords", new { id = model.ParentGroupID });
        }




        public IActionResult CreateGroupAsParent(int ChildGroupID, int wordId)
        {
            var model = new CreateGroupViewModel
            {
                ParentGroupID = ChildGroupID
            };
                ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

            ViewBag.wordId = wordId;
            return View(model);
        }

        // Action to handle form submission
        [HttpPost]
        public async Task<IActionResult> CreateGroupAsParent(CreateGroupViewModel model, int wordId)
        {
            if (!ModelState.IsValid)
            {
                    ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

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
            return RedirectToAction("Details", "GroupWords", new { id = model.ParentGroupID });

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

            return RedirectToAction("Details", "GroupWords", new { id = wordId });
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
                return RedirectToAction("Details", "GroupWords", new { id = model.GroupId });

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
            return RedirectToAction("Details", "GroupWords", new { id = model.GroupId });
        }






        // public IActionResult AddWordToGroup(int groupid)
        // {
        //     // GroupID - Combine Name, OriginLanguage, and EtymologyWord for the display field
        //     ViewData["GroupID"] = new SelectList(_context.Groups.Select(g => new {
        //         g.ID,
        //         DisplayField = g.Name + " (" + g.OriginLanguage + ", " + g.EtymologyWord + ")"
        //     }), "ID", "DisplayField");

        //     // RootID - Only words that start with "C-"
        //     ViewData["RootID"] = new SelectList(_context.Words
        //         .Where(w => w.Language.StartsWith("C-"))
        //         .Select(w => new {
        //             w.WordId,
        //             DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //         }), "WordId", "DisplayField");

        //     ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
        //     TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

        //     ViewBag.GroupID = groupid;
        //     return View();
        // }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWordForGroup(int groupId, [Bind("Word_text,Language,Class,notes,IPA,Pronunciation,IsDrevWord,RootID")] Word word)
        {
            if (ModelState.IsValid)
            {
                // Handle null values for foreign keys
                if (word.RootID == 0) word.RootID = null;

                // Validate that if RootID is provided, it's a valid Coptic root word
                if (word.RootID.HasValue)
                {
                    var rootWord = await _context.Words.FindAsync(word.RootID.Value);
                    if (rootWord == null || rootWord.RootID != null || !rootWord.Language.StartsWith("C-"))
                    {
                        ModelState.AddModelError("RootID", "Invalid root word selected. Only Coptic root words are allowed.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Set the GroupID to the current group
                    word.GroupID = groupId;

                    _context.Add(word);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"Word '{word.Word_text}' has been created and added to the group successfully.";

                    // Redirect back to group details
                    var returnUrl = TempData["ReturnUrl"] as string;
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Details", new { id = groupId });
                }
            }

            // Repopulate dropdowns if validation fails - ONLY Coptic root words
            var group = await _context.Groups.FindAsync(groupId);
            ViewBag.GroupName = group?.Name ?? "Unknown Group";
            ViewBag.GroupId = groupId;
            ViewBag.GroupOrigin = group?.OriginLanguage;
            ViewBag.GroupEtymology = group?.EtymologyWord;
            ViewBag.GroupNotes = group?.Notes;
            ViewBag.GroupWordCount = _context.Words.Count(w => w.GroupID == groupId);

            var roots = _context.Words
                .Where(w => w.RootID == null && w.Language.StartsWith("C-")) // Only Coptic root words
                .Select(w => new
                {
                    WordId = (int?)w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
                }).ToList();
            roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
            ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField", word.RootID);

            ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text", word.Language);
            ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text", word.Class);

            return View(word);
        }

        // KEEP THIS METHOD (it's correct):
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWordToGroup(int wordId, int groupId)
        {
            var word = await _context.Words.FindAsync(wordId);
            if (word == null)
            {
                TempData["Error"] = "Word not found.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Update the word's GroupID
            word.GroupID = groupId;
            _context.Update(word);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Word '{word.Word_text}' has been added to the group successfully.";
            return RedirectToAction("Details", new { id = groupId });
        }


        // Update AddExistingMeaningToGroup method to also filter parent meanings
public async Task<IActionResult> AddExistingMeaningToGroup(int groupId)
{
    var meanings = _context.Meanings
        .Where(m => m.ParentMeaningID == null) // Only parent meanings
        .Include(m => m.WordMeanings) // Include related WordMeanings
        .ThenInclude(wm => wm.Word)   // Include related Word for each WordMeaning
        .ToList();

    // Create a list of SelectListItem with formatted text
    var selectListItems = meanings.Select(m => new SelectListItem
    {
        Value = m.ID.ToString(), // Use Meaning ID as the value
        Text = $"{m.MeaningText} - [{string.Join(", ", m.WordMeanings.Select(wm => wm.Word.Word_text))}]" // Format the text
    }).ToList();

    // Pass the formatted SelectList to the view
    ViewBag.Meanings = new SelectList(selectListItems, "Value", "Text");
    ViewBag.groupId = groupId; // Pass the groupId to the view
    return View();
}

        [HttpPost]
        public async Task<IActionResult> AddExistingMeaningToGroup(int meaningID, int groupId)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "The selected word does not exist.");
                ViewBag.Meanings = new SelectList(_context.Meanings.ToList(), "ID", "MeaningText");
                ViewBag.groupId = groupId;
                return View();
            }

            // Get all words in the specified group
            var words = await _context.Words
                .Where(w => w.GroupID == groupId)
                .ToListAsync();

            if (words == null || !words.Any())
            {
                TempData["Error"] = "No words found in the specified group.";
                return RedirectToAction("Details", "GroupWords", new { id = groupId });
            }

            // Add the existing meaning to each word in the group
            foreach (var word in words)
            {
                var wordMeaning = new WordMeaning
                {
                    WordID = word.WordId,
                    MeaningID = meaningID
                };
                _context.WordMeanings.Add(wordMeaning);
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Existing meaning added to all words in the group successfully.";
            return RedirectToAction("Details", "GroupWords", new { id = groupId });
        }

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

        public async Task<IActionResult> SelectExistingGroupAsChild(int parentGroupID, string? search, string? groupSearchType = "contain", string? wordSearchType = "contain")
        {
            var parentGroup = await _context.Groups.FindAsync(parentGroupID);
            ViewBag.ParentGroupID = parentGroupID;
            ViewBag.ParentGroupName = parentGroup?.Name ?? "Unknown Group";
            ViewBag.SearchText = search;
            ViewBag.GroupSearchType = groupSearchType;
            ViewBag.WordSearchType = wordSearchType;

            if (string.IsNullOrEmpty(search))
            {
                ViewBag.AvailableGroups = new List<GroupWord>();
                return View();
            }

            // Normalize the search string
            search = NormalizeString(search);

            // Get existing child IDs to exclude them
            var existingChildIds = await _context.GroupRelations
                .Where(gr => gr.ParentGroupID == parentGroupID)
                .Select(gr => gr.RelatedGroupID)
                .ToListAsync();

            // Get all groups with their words, excluding the parent group itself and existing children
            var allGroups = await _context.Groups
                .Include(g => g.Words)
                .Where(g => g.ID != parentGroupID && !existingChildIds.Contains(g.ID))
                .ToListAsync();

            // Filter groups based on search criteria
            var filteredGroups = allGroups
                .Where(g =>
                    // Search in group name based on groupSearchType
                    MatchGroupName(g.Name, search, groupSearchType) ||
                    // Search in words within the group based on wordSearchType
                    (g.Words != null && g.Words.Any(w => MatchWordText(w.Word_text, search, wordSearchType)))
                )
                .OrderBy(g => g.Name)
                .ToList();

            ViewBag.AvailableGroups = filteredGroups;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingGroupAsChild(int parentGroupID, int childGroupID, bool? isCompound)
        {
            // Check if relation already exists
            var existingRelation = await _context.GroupRelations
                .FirstOrDefaultAsync(gr => gr.ParentGroupID == parentGroupID && gr.RelatedGroupID == childGroupID);

            if (existingRelation != null)
            {
                TempData["Error"] = "This group relation already exists.";
                return RedirectToAction("Details", new { id = parentGroupID });
            }

            // Create the GroupRelation
            var groupRelation = new GroupRelation
            {
                ParentGroupID = parentGroupID,
                RelatedGroupID = childGroupID,
                IsCompound = isCompound
            };

            _context.GroupRelations.Add(groupRelation);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Group relation created successfully.";
            return RedirectToAction("Details", new { id = parentGroupID });
        }

        public async Task<IActionResult> SelectExistingGroupAsParent(int childGroupID, string? search, string? groupSearchType = "contain", string? wordSearchType = "contain")
        {
            var childGroup = await _context.Groups.FindAsync(childGroupID);
            ViewBag.ChildGroupID = childGroupID;
            ViewBag.ChildGroupName = childGroup?.Name ?? "Unknown Group";
            ViewBag.SearchText = search;
            ViewBag.GroupSearchType = groupSearchType;
            ViewBag.WordSearchType = wordSearchType;

            if (string.IsNullOrEmpty(search))
            {
                ViewBag.AvailableGroups = new List<GroupWord>();
                return View();
            }

            // Normalize the search string
            search = NormalizeString(search);

            // Get existing parent IDs to exclude them
            var existingParentIds = await _context.GroupRelations
                .Where(gr => gr.RelatedGroupID == childGroupID)
                .Select(gr => gr.ParentGroupID)
                .ToListAsync();

            // Get all groups with their words, excluding the child group itself and existing parents
            var allGroups = await _context.Groups
                .Include(g => g.Words)
                .Where(g => g.ID != childGroupID && !existingParentIds.Contains(g.ID))
                .ToListAsync();

            // Filter groups based on search criteria
            var filteredGroups = allGroups
                .Where(g =>
                    // Search in group name based on groupSearchType
                    MatchGroupName(g.Name, search, groupSearchType) ||
                    // Search in words within the group based on wordSearchType
                    (g.Words != null && g.Words.Any(w => MatchWordText(w.Word_text, search, wordSearchType)))
                )
                .OrderBy(g => g.Name)
                .ToList();

            ViewBag.AvailableGroups = filteredGroups;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingGroupAsParent(int childGroupID, int parentGroupID, bool? isCompound)
        {
            // Check if relation already exists
            var existingRelation = await _context.GroupRelations
                .FirstOrDefaultAsync(gr => gr.ParentGroupID == parentGroupID && gr.RelatedGroupID == childGroupID);

            if (existingRelation != null)
            {
                TempData["Warning"] = "This parent group relation already exists.";
                return RedirectToAction("Details", new { id = childGroupID });
            }

            var newRelation = new GroupRelation
            {
                ParentGroupID = parentGroupID,
                RelatedGroupID = childGroupID,
                IsCompound = isCompound
            };

            _context.GroupRelations.Add(newRelation);
            await _context.SaveChangesAsync();

            var parentGroup = await _context.Groups.FindAsync(parentGroupID);
            TempData["Message"] = $"Group '{parentGroup?.Name}' has been added as a parent group successfully.";

            return RedirectToAction("Details", new { id = childGroupID });
        }
        // Helper methods for search matching
        private bool MatchGroupName(string groupName, string search, string searchType)
        {
            if (string.IsNullOrEmpty(groupName)) return false;

            var normalizedGroupName = NormalizeString(groupName);

            switch (searchType)
            {
                case "exact":
                    return normalizedGroupName == search;
                case "contain":
                    return normalizedGroupName.Contains(search);
                case "start":
                    return normalizedGroupName.StartsWith(search);
                case "end":
                    return normalizedGroupName.EndsWith(search);
                default:
                    return normalizedGroupName.Contains(search);
            }
        }

        private bool MatchWordText(string wordText, string search, string searchType)
        {
            if (string.IsNullOrEmpty(wordText)) return false;

            var normalizedWordText = NormalizeString(wordText);

            switch (searchType)
            {
                case "exact":
                    return normalizedWordText == search;
                case "contain":
                    return normalizedWordText.Contains(search);
                case "start":
                    return normalizedWordText.StartsWith(search);
                case "end":
                    return normalizedWordText.EndsWith(search);
                default:
                    return normalizedWordText.Contains(search);
            }
        }
        // GET: GroupWords/SelectExistingGroupAsChild
        //public async Task<IActionResult> SelectExistingGroupAsChild(int parentGroupID, string? search)
        //{
        //    ViewBag.ParentGroupID = parentGroupID;
        //    ViewBag.SearchText = search;

        //    // Get the parent group name for display
        //    var parentGroup = await _context.Groups.FindAsync(parentGroupID);
        //    ViewBag.ParentGroupName = parentGroup?.Name ?? "Unknown Group";

        //    if (string.IsNullOrEmpty(search))
        //    {
        //        return View(new List<GroupWord>());
        //    }

        //    // Get groups that are not already children of this group and not the group itself
        //    var existingChildIds = await _context.GroupRelations
        //        .Where(gr => gr.ParentGroupID == parentGroupID)
        //        .Select(gr => gr.RelatedGroupID)
        //        .ToListAsync();

        //    var availableGroups = await _context.Groups
        //        .Where(g => g.ID != parentGroupID && !existingChildIds.Contains(g.ID))
        //        .Where(g => g.Name.Contains(search))
        //        .OrderBy(g => g.Name)
        //        .ToListAsync();

        //    return View(availableGroups);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddExistingGroupAsChild(int parentGroupID, int childGroupID)
        //{
        //    // Check if relation already exists
        //    var existingRelation = await _context.GroupRelations
        //        .FirstOrDefaultAsync(gr => gr.ParentGroupID == parentGroupID && gr.RelatedGroupID == childGroupID);

        //    if (existingRelation != null)
        //    {
        //        TempData["Warning"] = "This child group relation already exists.";
        //        return RedirectToAction("Details", new { id = parentGroupID });
        //    }

        //    var newRelation = new GroupRelation
        //    {
        //        ParentGroupID = parentGroupID,
        //        RelatedGroupID = childGroupID
        //    };

        //    _context.GroupRelations.Add(newRelation);
        //    await _context.SaveChangesAsync();

        //    var childGroup = await _context.Groups.FindAsync(childGroupID);
        //    TempData["Message"] = $"Group '{childGroup?.Name}' has been added as a child group successfully.";

        //    return RedirectToAction("Details", new { id = parentGroupID });
        //}

        // ...existing code...

        // Helper methods - Add these to your GroupWordsController


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
        new SelectListItem { Value = "ⲣⲁϩ", Text = "Verb (imperative) - فعل (صيغة أمر)" },
        new SelectListItem { Value = "ⲥ", Text = "adjective - صفة" },
        new SelectListItem { Value = "ⲡ,ⲧ", Text = "Masculine or Feminine (noun) - اسم مذكر او مؤنث" },
        new SelectListItem { Value = "ϭⲱⲣ", Text = "Demonstrative pronoun - اسم اشارة" },
        new SelectListItem { Value = "ⲡⲧⲙ", Text = "Relative pronoun - اسم موصول" },
        new SelectListItem { Value = "ϣⲓⲛ", Text = "interrogative adverb - أداة استفهام" },
        new SelectListItem { Value = "ϣ", Text = "Letter - حرف" },
        new SelectListItem { Value = "ϣⲣ", Text = "Conjunction -  حرف عطف" },
        new SelectListItem { Value = "ϣⲥ", Text = "Preposition - حرف جر" },
        new SelectListItem { Value = "ϣϫ", Text = "negative particle - حرف نفى" },
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



        // GET: GroupWords/SelectWordForGroup
        // GET: GroupWords/SelectWordForGroup
        public async Task<IActionResult> SelectWordForGroup(int groupId, string? search, string? searchType = "contain")
        {
            ViewBag.GroupId = groupId;
            ViewBag.SearchText = search;
            ViewBag.SearchType = searchType;

            // Get the group name for display
            var group = await _context.Groups.FindAsync(groupId);
            ViewBag.GroupName = group?.Name ?? "Unknown Group";

            if (string.IsNullOrEmpty(search))
            {
                return View(new List<Word>());
            }

            // Normalize the search string
            search = NormalizeString(search);

            // Get words that are not already in this group AND have Language starting with "C-" (Coptic)
            var wordsQuery = _context.Words
                .Include(w => w.GroupWord) // Include current group info
                .Where(w => (w.GroupID != groupId || w.GroupID == null) && w.Language.StartsWith("C-")) // Exclude words already in this group AND filter for Coptic languages only
                .ToList();

            // Apply the search type
            List<Word> filteredWords;
            switch (searchType)
            {
                case "exact":
                    // Exact match
                    filteredWords = wordsQuery.Where(w => NormalizeString(w.Word_text) == search).ToList();
                    break;

                case "contain":
                    // Contains search term
                    filteredWords = wordsQuery.Where(w => NormalizeString(w.Word_text).Contains(search)).ToList();
                    break;

                case "start":
                    // Starts with search term
                    filteredWords = wordsQuery.Where(w => NormalizeString(w.Word_text).StartsWith(search)).ToList();
                    break;

                case "end":
                    // Ends with search term
                    filteredWords = wordsQuery.Where(w => NormalizeString(w.Word_text).EndsWith(search)).ToList();
                    break;

                default:
                    // Default to contains search if no valid search type is provided
                    filteredWords = wordsQuery.Where(w => NormalizeString(w.Word_text).Contains(search)).ToList();
                    break;
            }

            // Order the results
            filteredWords = filteredWords.OrderBy(w => w.Word_text).ToList();

            return View(filteredWords);
        }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> AddWordToGroup(int wordId, int groupId)
        // {
        //     var word = await _context.Words.FindAsync(wordId);
        //     if (word == null)
        //     {
        //         TempData["Error"] = "Word not found.";
        //         return RedirectToAction("Details", new { id = groupId });
        //     }

        //     // Update the word's GroupID
        //     word.GroupID = groupId;
        //     _context.Update(word);
        //     await _context.SaveChangesAsync();

        //     TempData["Message"] = $"Word '{word.Word_text}' has been added to the group successfully.";
        //     return RedirectToAction("Details", new { id = groupId });
        // }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromGroup(int wordId, int groupId)
        {
            var word = await _context.Words.FindAsync(wordId);
            if (word == null)
            {
                TempData["Error"] = "Word not found.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Set GroupID to null to remove from group
            word.GroupID = null;
            _context.Update(word);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Word '{word.Word_text}' has been removed from the group successfully.";
            return RedirectToAction("Details", new { id = groupId });
        }

        // GET: GroupWords/CreateWordForGroup
        //public IActionResult CreateWordForGroup(int groupId)
        //{
        //    TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

        //    // Get the group name for display
        //    var group = _context.Groups.Find(groupId);
        //    ViewBag.GroupName = group?.Name ?? "Unknown Group";
        //    ViewBag.GroupId = groupId;

        //    // Populate RootID dropdown
        //    var roots = _context.Words
        //        .Where(w => w.Language.StartsWith("C-"))
        //        .Select(w => new {
        //            WordId = (int?)w.WordId,
        //            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //        }).ToList();
        //    roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
        //    ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField");

        //    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
        //    ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

        //    return View();
        //}


        // ...existing code...
        // GET: GroupWords/AddDefinitionToAllWords
        // GET: GroupWords/AddDefinitionToAllWords
        public async Task<IActionResult> AddDefinitionToAllWords(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Words)
                .FirstOrDefaultAsync(g => g.ID == groupId);

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.WordCount = group.Words?.Count ?? 0;
            ViewBag.GroupWords = group.Words?.ToList() ?? new List<Word>();
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDefinitionToAllWords(int groupId, [Bind("MeaningText,Language,Notes")] Meaning meaning)
        {
            // Debug: Check what's being received
            System.Diagnostics.Debug.WriteLine($"GroupId: {groupId}");
            System.Diagnostics.Debug.WriteLine($"MeaningText: '{meaning.MeaningText}'");
            System.Diagnostics.Debug.WriteLine($"Language: '{meaning.Language}'");
            System.Diagnostics.Debug.WriteLine($"Notes: '{meaning.Notes}'");

            // Debug: Check ModelState
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    var key = modelState.Key;
                    var errors = modelState.Value.Errors;
                    if (errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error - Key: {key}, Errors: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get all words in the group
                    var wordsInGroup = await _context.Words
                        .Where(w => w.GroupID == groupId)
                        .ToListAsync();

                    if (!wordsInGroup.Any())
                    {
                        TempData["Error"] = "No words found in this group.";
                        return RedirectToAction("Details", new { id = groupId });
                    }

                    // Create the meaning
                    _context.Meanings.Add(meaning);
                    await _context.SaveChangesAsync();

                    // Create WordMeaning relationships for all words in the group
                    var wordMeanings = new List<WordMeaning>();
                    foreach (var word in wordsInGroup)
                    {
                        wordMeanings.Add(new WordMeaning
                        {
                            WordID = word.WordId,
                            MeaningID = meaning.ID  // Use ID, not MeaningId
                        });
                    }

                    _context.WordMeanings.AddRange(wordMeanings);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"Definition has been successfully added to all {wordsInGroup.Count} words in the group.";
                    return RedirectToAction("Details", new { id = groupId });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"An error occurred while adding the definition: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            var group = await _context.Groups
                .Include(g => g.Words)
                .FirstOrDefaultAsync(g => g.ID == groupId);

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group?.Name ?? "Unknown Group";
            ViewBag.WordCount = group?.Words?.Count ?? 0;
            ViewBag.GroupWords = group?.Words?.ToList() ?? new List<Word>();
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text", meaning.Language);

            return View(meaning);
        }
        // GET: GroupWords/CreateWordForGroup
        //public IActionResult CreateWordForGroup(int groupId)
        //{
        //    TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

        //    // Get the group name for display
        //    var group = _context.Groups.Find(groupId);
        //    ViewBag.GroupName = group?.Name ?? "Unknown Group";
        //    ViewBag.GroupId = groupId;

        //    // Populate RootID dropdown
        //    var roots = _context.Words
        //        .Where(w => w.Language.StartsWith("C-"))
        //        .Select(w => new {
        //            WordId = (int?)w.WordId,
        //            DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
        //        }).ToList();
        //    roots.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
        //    ViewData["RootID"] = new SelectList(roots, "WordId", "DisplayField");

        //    ViewData["Languages"] = new SelectList(GetLanguagesList(), "Value", "Text");
        //    ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

        //    return View();
        //}

        // GET: GroupWords/CreateWordForGroup
        public IActionResult CreateWordForGroup(int groupId, string RootSearch = "")
        {
            TempData["ReturnUrl"] = Request.Headers["Referer"].ToString();

            // Get the group information for display
            var group = _context.Groups.Find(groupId);
            if (group == null)
            {
                return NotFound();
            }

            // Get word count for the group
            var wordCount = _context.Words.Count(w => w.GroupID == groupId);

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.GroupOrigin = group.OriginLanguage;
            ViewBag.GroupEtymology = group.EtymologyWord;
            ViewBag.GroupNotes = group.Notes;
            ViewBag.GroupWordCount = wordCount;

            // Normalize the root search term
            RootSearch = NormalizeString(RootSearch);
            ViewData["RootSearch"] = RootSearch;

            // Populate RootID dropdown with search filter - ONLY Coptic words with RootID = null
            IEnumerable<Word> rootWordsQuery = _context.Words
                .Where(w => w.RootID == null && w.Language.StartsWith("C-")) // Only Coptic root words
                .AsEnumerable();

            if (!string.IsNullOrEmpty(RootSearch))
            {
                // Apply normalization and filtering in memory
                rootWordsQuery = rootWordsQuery.Where(w => NormalizeString(w.Word_text).Contains(RootSearch));
            }

            // Create the dropdown list
            var rootsList = rootWordsQuery.Select(w => new
            {
                WordId = (int?)w.WordId,
                DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")"
            }).ToList();

            rootsList.Insert(0, new { WordId = (int?)null, DisplayField = "No Root" });
            ViewData["RootID"] = new SelectList(rootsList, "WordId", "DisplayField");

            // Filter languages to show only Coptic languages (starting with "C-")
            ViewData["Languages"] = new SelectList(
                GetLanguagesList().Where(l => l.Value.StartsWith("C-")).ToList(),
                "Value",
                "Text"
            ); ViewData["Class"] = new SelectList(GetPartOfSpeechList(), "Value", "Text");

            return View();
        }



        [HttpGet]
        public JsonResult SearchRoots(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
            {
                return Json(new List<object>());
            }

            // Normalize the search term
            var normalizedSearch = NormalizeString(searchTerm);

            // Fetch ONLY Coptic root words (Language starts with "C-" AND RootID is null)
            var roots = _context.Words
                .Where(w => w.RootID == null && w.Language.StartsWith("C-")) // Only Coptic root words
                .AsEnumerable() // Switch to client-side evaluation for normalization
                .Where(w => NormalizeString(w.Word_text).Contains(normalizedSearch))
                .Select(w => new
                {
                    w.WordId,
                    DisplayField = w.Word_text + " (" + w.Language + ", " + w.Class + ")",
                    word_text = w.Word_text,
                    language = w.Language,
                    @class = w.Class,
                    notes = w.notes
                })
                .OrderBy(w => w.word_text)
                .Take(20) // Limit results
                .ToList();

            return Json(roots);
        }



        // GET: GroupWords/CreateGroupExplanation
        public async Task<IActionResult> CreateGroupExplanation(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.Languages = GetLanguagesList();

            return View();
        }

        // POST: GroupWords/CreateGroupExplanation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroupExplanation(GroupExplanation groupExplanation)
        {
            if (ModelState.IsValid)
            {
                _context.GroupExplanations.Add(groupExplanation);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Group explanation added successfully.";
                return RedirectToAction("Details", new { id = groupExplanation.GroupID });
            }

            var group = await _context.Groups.FindAsync(groupExplanation.GroupID);
            ViewBag.GroupId = groupExplanation.GroupID;
            ViewBag.GroupName = group?.Name ?? "Unknown Group";
            ViewBag.Languages = GetLanguagesList();

            return View(groupExplanation);
        }

        // GET: GroupWords/EditGroupExplanation
        public async Task<IActionResult> EditGroupExplanation(int id)
        {
            var groupExplanation = await _context.GroupExplanations
                .Include(ge => ge.GroupWord)
                .FirstOrDefaultAsync(ge => ge.ID == id);

            if (groupExplanation == null)
            {
                return NotFound();
            }

            ViewBag.Languages = GetLanguagesList();
            return View(groupExplanation);
        }

        // POST: GroupWords/EditGroupExplanation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroupExplanation(int id, GroupExplanation groupExplanation)
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
                    TempData["Message"] = "Group explanation updated successfully.";
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
                return RedirectToAction("Details", new { id = groupExplanation.GroupID });
            }

            ViewBag.Languages = GetLanguagesList();
            return View(groupExplanation);
        }

        // POST: GroupWords/DeleteGroupExplanation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroupExplanation(int id)
        {
            var groupExplanation = await _context.GroupExplanations.FindAsync(id);
            if (groupExplanation != null)
            {
                var groupId = groupExplanation.GroupID;
                _context.GroupExplanations.Remove(groupExplanation);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Group explanation deleted successfully.";
                return RedirectToAction("Details", new { id = groupId });
            }

            return NotFound();
        }

        // Helper method to check if GroupExplanation exists
        private bool GroupExplanationExists(int id)
        {
            return _context.GroupExplanations.Any(e => e.ID == id);
        }


        // ...existing code...

        public async Task<IActionResult> SelectExistingDefinitionForGroup(int groupId, string? search, string? definitionSearchType = "contain", string? wordSearchType = "contain")
{
    var group = await _context.Groups.FindAsync(groupId);
    ViewBag.GroupId = groupId;
    ViewBag.GroupName = group?.Name ?? "Unknown Group";
    ViewBag.SearchText = search;
    ViewBag.DefinitionSearchType = definitionSearchType;
    ViewBag.WordSearchType = wordSearchType;

    if (string.IsNullOrEmpty(search))
    {
        return View(new List<Meaning>());
    }

    try
    {
        // Normalize the search string
        search = NormalizeString(search);

        // Get existing meaning IDs already associated with the words in this group
        var wordsInGroup = await _context.Words
            .Where(w => w.GroupID == groupId)
            .Select(w => w.WordId)
            .ToListAsync();

        var existingMeaningIds = await _context.WordMeanings
            .Where(wm => wordsInGroup.Contains(wm.WordID))
            .Select(wm => wm.MeaningID)
            .ToListAsync();

        // Get all meanings with their associated words - ONLY parent meanings (ParentMeaningID is null)
        var allMeaningsWithWords = await _context.Meanings
            .Where(m => m.ParentMeaningID == null) // Only parent meanings
            .Include(m => m.WordMeanings)
            .ThenInclude(wm => wm.Word)
            .ToListAsync();

        // Filter meanings based on search criteria
        var filteredMeanings = allMeaningsWithWords
            .Where(m =>
                // Search in definition text based on definitionSearchType
                MatchDefinitionText(m.MeaningText, search, definitionSearchType) ||
                // Search in words within the meaning based on wordSearchType
                (m.WordMeanings != null && m.WordMeanings.Any(wm =>
                    wm.Word != null && MatchWordText(wm.Word.Word_text, search, wordSearchType)))
            )
            .OrderBy(m => m.MeaningText)
            .ToList();

        // Mark meanings that are already associated with this group's words
        foreach (var meaning in filteredMeanings)
        {
            if (existingMeaningIds.Contains(meaning.ID))
            {
                meaning.Notes = "Already linked to some words in this group";
            }
        }

        return View(filteredMeanings);
    }
    catch (Exception ex)
    {
        TempData["Error"] = "An error occurred while searching for definitions.";
        return View(new List<Meaning>());
    }
}

        private bool MatchDefinitionText(string definitionText, string search, string searchType)
        {
            if (string.IsNullOrEmpty(definitionText)) return false;

            var normalizedDefinitionText = NormalizeString(definitionText);

            return searchType switch
            {
                "exact" => normalizedDefinitionText == search,
                "start" => normalizedDefinitionText.StartsWith(search),
                "end" => normalizedDefinitionText.EndsWith(search),
                _ => normalizedDefinitionText.Contains(search)
            };
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingDefinitionToGroup(int meaningId, int groupId)
        {
            var meaning = await _context.Meanings.FindAsync(meaningId);
            if (meaning == null)
            {
                TempData["Error"] = "Definition not found.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Get all words in the group
            var wordsInGroup = await _context.Words
                .Where(w => w.GroupID == groupId)
                .ToListAsync();

            if (!wordsInGroup.Any())
            {
                TempData["Error"] = "No words found in this group.";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Get existing word-meaning associations to avoid duplicates
            var existingWordMeanings = await _context.WordMeanings
                .Where(wm => wm.MeaningID == meaningId &&
                       wordsInGroup.Select(w => w.WordId).Contains(wm.WordID))
                .Select(wm => wm.WordID)
                .ToListAsync();

            // Add the meaning to words that don't already have it
            var newWordMeanings = new List<WordMeaning>();
            foreach (var word in wordsInGroup)
            {
                if (!existingWordMeanings.Contains(word.WordId))
                {
                    newWordMeanings.Add(new WordMeaning
                    {
                        WordID = word.WordId,
                        MeaningID = meaningId
                    });
                }
            }

            if (newWordMeanings.Any())
            {
                _context.WordMeanings.AddRange(newWordMeanings);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Definition has been added to {newWordMeanings.Count} words in the group.";
            }
            else
            {
                TempData["Warning"] = "This definition is already associated with all words in the group.";
            }

            return RedirectToAction("Details", new { id = groupId });
        }




        // // Replace the AddDefinitionToWordsByClass methods with these corrected versions:

        // public async Task<IActionResult> AddDefinitionToWordsByClass(int groupId, string wordClass)
        // {
        //     var group = await _context.Groups
        //         .Include(g => g.Words)
        //         .FirstOrDefaultAsync(g => g.ID == groupId);

        //     if (group == null)
        //     {
        //         TempData["Error"] = "Group not found.";
        //         return RedirectToAction(nameof(Index));
        //     }

        //     // Get words in the group with the specific class
        //     var targetWords = group.Words?.Where(w => w.Class == wordClass).ToList() ?? new List<Word>();

        //     ViewBag.GroupId = groupId;
        //     ViewBag.GroupName = group.Name;
        //     ViewBag.WordClass = wordClass;
        //     ViewBag.WordCount = targetWords.Count;
        //     ViewBag.GroupWords = targetWords;
        //     ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

        //     return View();
        // }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> AddDefinitionToWordsByClass(int groupId, string wordClass, [Bind("MeaningText,Language,Notes")] Meaning meaning)
        // {
        //     if (ModelState.IsValid)
        //     {
        //         try
        //         {
        //             // Get words in the group with the specific class
        //             var targetWords = await _context.Words
        //                 .Where(w => w.GroupID == groupId && w.Class == wordClass)
        //                 .ToListAsync();

        //             if (!targetWords.Any())
        //             {
        //                 TempData["Error"] = $"No words found in this group with class '{wordClass}'.";
        //                 return RedirectToAction("Details", new { id = groupId });
        //             }

        //             // Create the meaning
        //             _context.Meanings.Add(meaning);
        //             await _context.SaveChangesAsync();

        //             // Create WordMeaning relationships for words with specific class
        //             var wordMeanings = targetWords.Select(word => new WordMeaning
        //             {
        //                 WordID = word.WordId,
        //                 MeaningID = meaning.ID
        //             }).ToList();

        //             _context.WordMeanings.AddRange(wordMeanings);
        //             await _context.SaveChangesAsync();

        //             TempData["Message"] = $"Definition has been successfully added to {targetWords.Count} words with class '{wordClass}' in the group.";
        //             return RedirectToAction("Details", new { id = groupId });
        //         }
        //         catch (Exception ex)
        //         {
        //             TempData["Error"] = "An error occurred while adding the definition.";
        //         }
        //     }

        //     var group = await _context.Groups
        //         .Include(g => g.Words)
        //         .FirstOrDefaultAsync(g => g.ID == groupId);

        //     var targetWordsForView = group?.Words?.Where(w => w.Class == wordClass).ToList() ?? new List<Word>();

        //     ViewBag.GroupId = groupId;
        //     ViewBag.GroupName = group?.Name ?? "Unknown Group";
        //     ViewBag.WordClass = wordClass;
        //     ViewBag.WordCount = targetWordsForView.Count;
        //     ViewBag.GroupWords = targetWordsForView;
        //     ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text", meaning.Language);

        //     return View(meaning);
        // }

        public async Task<IActionResult> AddDefinitionToWordsByClass(int groupId, string wordClass)
        {
            var group = await _context.Groups
                .Include(g => g.Words)
                .FirstOrDefaultAsync(g => g.ID == groupId);

            if (group == null)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction(nameof(Index));
            }

            // Handle "Unknown" class by treating it as null
            string actualClass = wordClass == "Unknown" ? null : wordClass;

            // Get words in the group with the specific class (including null values)
            var targetWords = group.Words?.Where(w =>
                (actualClass == null && w.Class == null) ||
                (actualClass != null && w.Class == actualClass)
            ).ToList() ?? new List<Word>();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.WordClass = wordClass; // Keep the display value
            ViewBag.WordCount = targetWords.Count;
            ViewBag.GroupWords = targetWords;
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDefinitionToWordsByClass(int groupId, string wordClass, [Bind("MeaningText,Language,Notes")] Meaning meaning)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle "Unknown" class by treating it as null
                    string classToMatch = wordClass == "Unknown" ? null : wordClass;

                    // Get words in the group with the specific class (including null values)
                    var targetWords = await _context.Words
                        .Where(w => w.GroupID == groupId &&
                            ((classToMatch == null && w.Class == null) ||
                             (classToMatch != null && w.Class == classToMatch)))
                        .ToListAsync();

                    if (!targetWords.Any())
                    {
                        TempData["Error"] = $"No words found in this group with class '{wordClass}'.";
                        return RedirectToAction("Details", new { id = groupId });
                    }

                    // Create the meaning
                    _context.Meanings.Add(meaning);
                    await _context.SaveChangesAsync();

                    // Create WordMeaning relationships for words with specific class
                    var wordMeanings = targetWords.Select(word => new WordMeaning
                    {
                        WordID = word.WordId,
                        MeaningID = meaning.ID
                    }).ToList();

                    _context.WordMeanings.AddRange(wordMeanings);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"Definition has been successfully added to {targetWords.Count} words with class '{wordClass}' in the group.";
                    return RedirectToAction("Details", new { id = groupId });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An error occurred while adding the definition.";
                }
            }

            var group = await _context.Groups
                .Include(g => g.Words)
                .FirstOrDefaultAsync(g => g.ID == groupId);

            // Handle "Unknown" class by treating it as null for view repopulation
            string classForView = wordClass == "Unknown" ? null : wordClass;

            var targetWordsForView = group?.Words?.Where(w =>
                (classForView == null && w.Class == null) ||
                (classForView != null && w.Class == classForView)
            ).ToList() ?? new List<Word>();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group?.Name ?? "Unknown Group";
            ViewBag.WordClass = wordClass;
            ViewBag.WordCount = targetWordsForView.Count;
            ViewBag.GroupWords = targetWordsForView;
            ViewBag.Languages = new SelectList(GetLanguagesList(), "Value", "Text", meaning.Language);

            return View(meaning);
        }

        // GET: Select Existing Definition for Words by Class
        //         public async Task<IActionResult> SelectExistingDefinitionForWordsByClass(int groupId, string wordClass, string? search, string? definitionSearchType = "contain", string? wordSearchType = "contain")
        //         {
        //             var group = await _context.Groups.FindAsync(groupId);
        //             ViewBag.GroupId = groupId;
        //             ViewBag.GroupName = group?.Name ?? "Unknown Group";
        //             ViewBag.WordClass = wordClass;
        //             ViewBag.SearchText = search;
        //             ViewBag.DefinitionSearchType = definitionSearchType;
        //             ViewBag.WordSearchType = wordSearchType;

        //             // Get count of words with this class in the group
        //             var wordsWithClassCount = await _context.Words
        //                 .CountAsync(w => w.GroupID == groupId && w.Class == wordClass);
        //             ViewBag.WordsWithClassCount = wordsWithClassCount;

        //             if (string.IsNullOrEmpty(search))
        //             {
        //                 return View(new List<Meaning>());
        //             }

        //             try
        //             {
        //                 // Normalize the search string
        //                 search = NormalizeString(search);

        //                 // Get existing meaning IDs already associated with words of this class in this group
        //                 var targetWordIds = await _context.Words
        //                     .Where(w => w.GroupID == groupId && w.Class == wordClass)
        //                     .Select(w => w.WordId)
        //                     .ToListAsync();

        //                 var existingMeaningIds = await _context.WordMeanings
        //                     .Where(wm => targetWordIds.Contains(wm.WordID))
        //                     .Select(wm => wm.MeaningID)
        //                     .ToListAsync();

        //                 // Get all meanings with their associated words
        //                 var allMeaningsWithWords = await _context.Meanings
        //                     .Include(m => m.WordMeanings)
        //                     .ThenInclude(wm => wm.Word)
        //                     .ToListAsync();

        //                 // Filter meanings based on search criteria
        //                 var filteredMeanings = allMeaningsWithWords
        //                     .Where(m =>
        //                         // Search in definition text based on definitionSearchType
        //                         MatchDefinitionText(m.MeaningText, search, definitionSearchType) ||
        //                         // Search in words within the meaning based on wordSearchType
        //                         (m.WordMeanings != null && m.WordMeanings.Any(wm =>
        //                             wm.Word != null && MatchWordText(wm.Word.Word_text, search, wordSearchType)))
        //                     )
        //                     .OrderBy(m => m.MeaningText)
        //                     .ToList();

        //                 // Mark meanings that are already associated with words of this class in this group
        //                 foreach (var meaning in filteredMeanings)
        //                 {
        //                     if (existingMeaningIds.Contains(meaning.ID))
        //                     {
        //                         meaning.Notes = $"Already linked to some '{wordClass}' words in this group";
        //                     }
        //                 }

        //                 return View(filteredMeanings);
        //             }
        //             catch (Exception ex)
        //             {
        //                 TempData["Error"] = "An error occurred while searching for definitions.";
        //                 return View(new List<Meaning>());
        //             }
        //         }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> AddExistingDefinitionToWordsByClass(int meaningId, int groupId, string wordClass)
        // {
        //     try
        //     {
        //         var meaning = await _context.Meanings.FindAsync(meaningId);
        //         if (meaning == null)
        //         {
        //             TempData["Error"] = "Definition not found.";
        //             return RedirectToAction("Details", new { id = groupId });
        //         }

        //         // Get words in the group with the specific class
        //         var targetWords = await _context.Words
        //             .Where(w => w.GroupID == groupId && w.Class == wordClass)
        //             .ToListAsync();

        //         if (!targetWords.Any())
        //         {
        //             TempData["Error"] = $"No words found in this group with class '{wordClass}'.";
        //             return RedirectToAction("Details", new { id = groupId });
        //         }

        //         // Get existing word-meaning associations to avoid duplicates
        //         var existingWordMeanings = await _context.WordMeanings
        //             .Where(wm => wm.MeaningID == meaningId && 
        //                    targetWords.Select(w => w.WordId).Contains(wm.WordID))
        //             .Select(wm => wm.WordID)
        //             .ToListAsync();

        //         // Add the meaning to words that don't already have it
        //         var newWordMeanings = targetWords
        //             .Where(word => !existingWordMeanings.Contains(word.WordId))
        //             .Select(word => new WordMeaning
        //             {
        //                 WordID = word.WordId,
        //                 MeaningID = meaningId
        //             })
        //             .ToList();

        //         if (newWordMeanings.Any())
        //         {
        //             _context.WordMeanings.AddRange(newWordMeanings);
        //             await _context.SaveChangesAsync();
        //             TempData["Message"] = $"Definition has been added to {newWordMeanings.Count} words with class '{wordClass}' in the group.";
        //         }
        //         else
        //         {
        //             TempData["Warning"] = $"This definition is already associated with all '{wordClass}' words in the group.";
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         TempData["Error"] = "An error occurred while adding the definition to the words.";
        //     }

        //     return RedirectToAction("Details", new { id = groupId });
        // }
        public async Task<IActionResult> SelectExistingDefinitionForWordsByClass(int groupId, string wordClass, string? search, string? definitionSearchType = "contain", string? wordSearchType = "contain")
{
    var group = await _context.Groups.FindAsync(groupId);
    ViewBag.GroupId = groupId;
    ViewBag.GroupName = group?.Name ?? "Unknown Group";
    ViewBag.WordClass = wordClass;
    ViewBag.SearchText = search;
    ViewBag.DefinitionSearchType = definitionSearchType;
    ViewBag.WordSearchType = wordSearchType;

    // Handle "Unknown" class by treating it as null
    string actualClass = wordClass == "Unknown" ? null : wordClass;

    // Get count of words with this class in the group (including null values)
    var wordsWithClassCount = await _context.Words
        .CountAsync(w => w.GroupID == groupId && 
            ((actualClass == null && w.Class == null) || 
             (actualClass != null && w.Class == actualClass)));
    ViewBag.WordsWithClassCount = wordsWithClassCount;

    if (string.IsNullOrEmpty(search))
    {
        return View(new List<Meaning>());
    }

    try
    {
        // Normalize the search string
        search = NormalizeString(search);

        // Get existing meaning IDs already associated with words of this class in this group
        var targetWordIds = await _context.Words
            .Where(w => w.GroupID == groupId && 
                ((actualClass == null && w.Class == null) || 
                 (actualClass != null && w.Class == actualClass)))
            .Select(w => w.WordId)
            .ToListAsync();

        var existingMeaningIds = await _context.WordMeanings
            .Where(wm => targetWordIds.Contains(wm.WordID))
            .Select(wm => wm.MeaningID)
            .ToListAsync();

        // Get all meanings with their associated words - ONLY parent meanings (ParentMeaningID is null)
        var allMeaningsWithWords = await _context.Meanings
            .Where(m => m.ParentMeaningID == null) // Only parent meanings
            .Include(m => m.WordMeanings)
            .ThenInclude(wm => wm.Word)
            .ToListAsync();

        // Filter meanings based on search criteria
        var filteredMeanings = allMeaningsWithWords
            .Where(m =>
                // Search in definition text based on definitionSearchType
                MatchDefinitionText(m.MeaningText, search, definitionSearchType) ||
                // Search in words within the meaning based on wordSearchType
                (m.WordMeanings != null && m.WordMeanings.Any(wm => 
                    wm.Word != null && MatchWordText(wm.Word.Word_text, search, wordSearchType)))
            )
            .OrderBy(m => m.MeaningText)
            .ToList();

        // Mark meanings that are already associated with words of this class in this group
        foreach (var meaning in filteredMeanings)
        {
            if (existingMeaningIds.Contains(meaning.ID))
            {
                meaning.Notes = $"Already linked to some '{wordClass}' words in this group";
            }
        }

        return View(filteredMeanings);
    }
    catch (Exception ex)
    {
        TempData["Error"] = "An error occurred while searching for definitions.";
        return View(new List<Meaning>());
    }
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddExistingDefinitionToWordsByClass(int meaningId, int groupId, string wordClass)
{
    try
    {
        var meaning = await _context.Meanings.FindAsync(meaningId);
        if (meaning == null)
        {
            TempData["Error"] = "Definition not found.";
            return RedirectToAction("Details", new { id = groupId });
        }

        // Handle "Unknown" class by treating it as null
        string actualClass = wordClass == "Unknown" ? null : wordClass;

        // Get words in the group with the specific class (including null values)
        var targetWords = await _context.Words
            .Where(w => w.GroupID == groupId && 
                ((actualClass == null && w.Class == null) || 
                 (actualClass != null && w.Class == actualClass)))
            .ToListAsync();

        if (!targetWords.Any())
        {
            TempData["Error"] = $"No words found in this group with class '{wordClass}'.";
            return RedirectToAction("Details", new { id = groupId });
        }

        // Get existing word-meaning associations to avoid duplicates
        var existingWordMeanings = await _context.WordMeanings
            .Where(wm => wm.MeaningID == meaningId && 
                   targetWords.Select(w => w.WordId).Contains(wm.WordID))
            .Select(wm => wm.WordID)
            .ToListAsync();

        // Add the meaning to words that don't already have it
        var newWordMeanings = targetWords
            .Where(word => !existingWordMeanings.Contains(word.WordId))
            .Select(word => new WordMeaning
            {
                WordID = word.WordId,
                MeaningID = meaningId
            })
            .ToList();

        if (newWordMeanings.Any())
        {
            _context.WordMeanings.AddRange(newWordMeanings);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Definition has been added to {newWordMeanings.Count} words with class '{wordClass}' in the group.";
        }
        else
        {
            TempData["Warning"] = $"This definition is already associated with all '{wordClass}' words in the group.";
        }
    }
    catch (Exception ex)
    {
        TempData["Error"] = "An error occurred while adding the definition to the words.";
    }

    return RedirectToAction("Details", new { id = groupId });
}

    }
}