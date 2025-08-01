public class DatabaseService
{
    private readonly ApplicationDbContext _context;

    public DatabaseService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Make sure this method is defined
    public void InsertAllData(ExcelImporter importer)
    {
        // Insert Meanings first
        _context.Bibles.AddRange(importer.Bible);
        _context.SaveChanges();
        _context.Dictionaries.AddRange(importer.Dictionary);
        _context.SaveChanges();
        _context.Groups.AddRange(importer.Group);
        _context.SaveChanges();
        _context.Meanings.AddRange(importer.Meanings);
        _context.SaveChanges();



        // Insert Words
        _context.Words.AddRange(importer.Words);
        _context.SaveChanges();


        //Validate and Insert WordMeanings
        var validMeaningIds = _context.Meanings.Select(m => m.ID).ToHashSet();

        var validWordMeanings = importer.WordMeanings.Where(wm => validMeaningIds.Contains(wm.MeaningID)).ToList();

        if (validWordMeanings.Count != importer.WordMeanings.Count)
        {
            Console.WriteLine("Some WordMeaning records have invalid MeaningID values.");
        }

        _context.WordMeanings.AddRange(validWordMeanings);
        _context.SaveChanges();
        _context.DictionaryReferenceWords.AddRange(importer.DictionaryReferenceWords);
        _context.SaveChanges();

        // Insert DictionaryReferenceWords

    }

}