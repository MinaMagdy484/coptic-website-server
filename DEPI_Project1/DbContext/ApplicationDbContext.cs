using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DEPI_Project1.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using System.Security.Claims;
using DEPI_Project1.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INormalizationService _normalizationService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor, INormalizationService normalizationService)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _normalizationService = normalizationService;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost;Database=Coptic(with login)8;Trusted_Connection = True;TrustServerCertificate=True;");
        //optionsBuilder.UseSqlServer("Server=10.0.0.4,1433;Database=coptic_dictionary4;User Id=AppUser;Password=Coptic123;TrustServerCertificate=True;",
         //   options => options.CommandTimeout(120));

        base.OnConfiguring(optionsBuilder);
    }
    //public DbSet<UserType> UserTypes { get; set; }


    public DbSet<Word> Words { get; set; }
    public DbSet<DictionaryReferenceWord> DictionaryReferenceWords { get; set; }
    public DbSet<Dictionary> Dictionaries { get; set; }
    public DbSet<GroupWord> Groups { get; set; }
    public DbSet<GroupExplanation> GroupExplanations { get; set; }
    public DbSet<GroupRelation> GroupRelations { get; set; } // Added GroupRelation DbSet

    public DbSet<WordExplanation> WordExplanations { get; set; }

    public DbSet<WordMeaning> WordMeanings { get; set; }
    public DbSet<Meaning> Meanings { get; set; }
    public DbSet<Example> Examples { get; set; }
    public DbSet<WordMeaningBible> WordMeaningBibles { get; set; }
    public DbSet<Bible> Bibles { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region Data Seeding
        //new DbInitializer(modelBuilder).Seed();
        modelBuilder.Entity<IdentityRole>().HasData(
                   new IdentityRole()
                   {
                       Id = Guid.NewGuid().ToString(),
                       Name = "Admin",
                       NormalizedName = "Admin".ToUpper(),
                       ConcurrencyStamp = "Admin".ToUpper(),
                   },
                   new IdentityRole()
                   {
                       Id = Guid.NewGuid().ToString(),
                       Name = "User",
                       NormalizedName = "User".ToUpper(),
                       ConcurrencyStamp = "User".ToUpper(),
                   },
                    new IdentityRole()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Student",
                        NormalizedName = "Student".ToUpper(),
                        ConcurrencyStamp = "Student".ToUpper(),
                    },
                     new IdentityRole()
                     {
                         Id = Guid.NewGuid().ToString(),
                         Name = "Instructor",
                         NormalizedName = "Instructor".ToUpper(),
                         ConcurrencyStamp = "Instructor".ToUpper(),
                     }

        );
        #endregion

        modelBuilder.Ignore<Capture>();

        // Word to Group (One-to-Many)
        modelBuilder.Entity<Word>()
            .HasOne(w => w.GroupWord)
            .WithMany(g => g.Words)
            .HasForeignKey(w => w.GroupID);

        // Word to DictionaryReferenceWord (One-to-Many)
        modelBuilder.Entity<DictionaryReferenceWord>()
            .HasOne(drw => drw.Word)
            .WithMany(w => w.DictionaryReferenceWords)
            .HasForeignKey(drw => drw.WordID);

        // DictionaryReferenceWord to DictionaryReference (Many-to-One)
        modelBuilder.Entity<DictionaryReferenceWord>()
            .HasOne(drw => drw.Dictionary)
            .WithMany(dr => dr.DictionaryReferenceWords)
            .HasForeignKey(drw => drw.DictionaryID);

        // Group to GroupExplanation (One-to-Many)
        modelBuilder.Entity<GroupExplanation>()
            .HasOne(ge => ge.GroupWord)
            .WithMany(g => g.GroupExplanations)
            .HasForeignKey(ge => ge.GroupID);

        // Word to WordExplanation (One-to-Many)
        modelBuilder.Entity<WordExplanation>()
            .HasOne(we => we.Word)
            .WithMany(w => w.WordExplanations)
            .HasForeignKey(we => we.WordID);

        // Word to WordMeaning (One-to-Many)
        modelBuilder.Entity<WordMeaning>()
            .HasOne(wm => wm.Word)
            .WithMany(w => w.WordMeanings)
            .HasForeignKey(wm => wm.WordID);

        // WordMeaning to Meaning (One-to-One)
        modelBuilder.Entity<WordMeaning>()
            .HasOne(wm => wm.Meaning)
            .WithMany(m => m.WordMeanings)
            .HasForeignKey(wm => wm.MeaningID);

        // WordMeaning to Example (One-to-Many)
        modelBuilder.Entity<Example>()
            .HasOne(e => e.WordMeaning)
            .WithMany(wm => wm.Examples)
            .HasForeignKey(e => e.WordMeaningID);

        // WordMeaning to WordMeaningBible (One-to-Many)
        modelBuilder.Entity<WordMeaningBible>()
            .HasOne(wmb => wmb.WordMeaning)
            .WithMany(wm => wm.WordMeaningBibles)
            .HasForeignKey(wmb => wmb.WordMeaningID);

        modelBuilder.Entity<WordMeaningBible>()
            .HasOne(wmb => wmb.Bible)
            .WithMany(wm => wm.WordMeaningBibles)
            .HasForeignKey(wmb => wmb.BibleID);



        // Self-referencing relation for Example (Parent-Child relationship)
        modelBuilder.Entity<Example>()
            .HasOne(e => e.ParentExample)
            .WithMany(e => e.ChildExamples)
            .HasForeignKey(e => e.ParentExampleID)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relation for Meaning (Parent-Child relationship)
        modelBuilder.Entity<Meaning>()
            .HasOne(m => m.ParentMeaning)
            .WithMany(m => m.ChildMeanings)
            .HasForeignKey(m => m.ParentMeaningID)
            .OnDelete(DeleteBehavior.Restrict);

        // GroupRelation (Self-Referencing Relationship)
        modelBuilder.Entity<GroupRelation>()
            .HasOne(gr => gr.ParentGroup)
            .WithMany(g => g.GroupChilds)
            .HasForeignKey(gr => gr.ParentGroupID)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<GroupRelation>()
            .HasOne(gr => gr.RelatedGroup)
            .WithMany(g => g.GroupParents)
            .HasForeignKey(gr => gr.RelatedGroupID)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Configure column sizes for text fields and their normalized versions
        modelBuilder.Entity<Bible>()
            .Property(b => b.Text)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Bible>()
            .Property(b => b.TextNormalized)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Word>()
            .Property(w => w.Word_text)
            .HasMaxLength(500);

        modelBuilder.Entity<Word>()
            .Property(w => w.Word_textNormalized)
            .HasMaxLength(500);

        modelBuilder.Entity<GroupWord>()
            .Property(g => g.Name)
            .HasMaxLength(500);

        modelBuilder.Entity<GroupWord>()
            .Property(g => g.NameNormalized)
            .HasMaxLength(500);

        modelBuilder.Entity<Meaning>()
            .Property(m => m.MeaningText)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Meaning>()
            .Property(m => m.MeaningTextNormalized)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Example>()
            .Property(e => e.ExampleText)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Example>()
            .Property(e => e.ExampleTextNormalized)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<WordExplanation>()
            .Property(w => w.Explanation)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<WordExplanation>()
            .Property(w => w.ExplanationNormalized)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<GroupExplanation>()
            .Property(g => g.Explanation)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<GroupExplanation>()
            .Property(g => g.ExplanationNormalized)
            .HasColumnType("nvarchar(max)");

        // Add indexes for normalized fields
        modelBuilder.Entity<Word>()
            .HasIndex(w => w.Word_textNormalized)
            .HasDatabaseName("IX_Word_Word_textNormalized");

        modelBuilder.Entity<Bible>()
            .HasIndex(b => b.TextNormalized)
            .HasDatabaseName("IX_Bible_TextNormalized");

        modelBuilder.Entity<GroupWord>()
            .HasIndex(g => g.NameNormalized)
            .HasDatabaseName("IX_GroupWord_NameNormalized");

        // ...rest of your existing OnModelCreating code...
        base.OnModelCreating(modelBuilder);
    }
     public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeEntities(); // Add this line
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedByUserId = userId;
                        entry.Entity.CreatedAt = now;
                        // Don't set Modified fields on creation
                        entry.Entity.ModifiedByUserId = null;
                        entry.Entity.ModifiedAt = null;
                        break;
                        
                    case EntityState.Modified:
                        // Get original values to preserve them
                        var originalCreatedBy = entry.OriginalValues[nameof(AuditableEntity.CreatedByUserId)];
                        var originalCreatedAt = entry.OriginalValues[nameof(AuditableEntity.CreatedAt)];
                        
                        // Restore original created values
                        entry.Entity.CreatedByUserId = originalCreatedBy?.ToString();
                        entry.Entity.CreatedAt = (DateTime)originalCreatedAt;
                        
                        // Mark created fields as not modified
                        entry.Property(nameof(AuditableEntity.CreatedByUserId)).IsModified = false;
                        entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                        
                        // Set modified fields
                        entry.Entity.ModifiedByUserId = userId;
                        entry.Entity.ModifiedAt = now;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            NormalizeEntities();
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedByUserId = userId;
                        entry.Entity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        // Preserve original Created values - don't modify them
                        entry.Property(nameof(AuditableEntity.CreatedByUserId)).IsModified = false;
                        entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                        
                        // Only update Modified values
                        entry.Entity.ModifiedByUserId = userId;
                        entry.Entity.ModifiedAt = now;
                        break;
                }
            }

            return base.SaveChanges();
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private void NormalizeEntities()
        {
            if (_normalizationService == null) return; // Add null check

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                _normalizationService.NormalizeEntity(entry.Entity);
            }
        }
}
