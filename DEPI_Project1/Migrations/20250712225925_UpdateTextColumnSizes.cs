using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DEPI_Project1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTextColumnSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bibles",
                columns: table => new
                {
                    BibleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Book = table.Column<int>(type: "int", nullable: false),
                    Chapter = table.Column<int>(type: "int", nullable: false),
                    Verse = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Edition = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    TextNormalized = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    Pronunciation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bibles", x => x.BibleID);
                    table.ForeignKey(
                        name: "FK_Bibles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bibles_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Dictionaries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DictionaryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detils = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxNumberOfPages = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dictionaries", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Dictionaries_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dictionaries_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EtymologyWord = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Etymology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Groups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Groups_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Meanings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeaningText = table.Column<string>(type: "nvarchar(1500)", maxLength: 600, nullable: false),
                    MeaningTextNormalized = table.Column<string>(type: "nvarchar(1500)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentMeaningID = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meanings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Meanings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meanings_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meanings_Meanings_ParentMeaningID",
                        column: x => x.ParentMeaningID,
                        principalTable: "Meanings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupExplanations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Explanation = table.Column<string>(type: "nvarchar(1500)", nullable: false),
                    ExplanationNormalized = table.Column<string>(type: "nvarchar(1500)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupExplanations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_GroupExplanations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupExplanations_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupExplanations_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupRelations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentGroupID = table.Column<int>(type: "int", nullable: false),
                    RelatedGroupID = table.Column<int>(type: "int", nullable: false),
                    IsCompound = table.Column<bool>(type: "bit", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupRelations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_GroupRelations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupRelations_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupRelations_Groups_ParentGroupID",
                        column: x => x.ParentGroupID,
                        principalTable: "Groups",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupRelations_Groups_RelatedGroupID",
                        column: x => x.RelatedGroupID,
                        principalTable: "Groups",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    WordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Word_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Word_textNormalized = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pronunciation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISCompleted = table.Column<bool>(type: "bit", nullable: true),
                    Review1 = table.Column<bool>(type: "bit", nullable: true),
                    Review2 = table.Column<bool>(type: "bit", nullable: true),
                    RootID = table.Column<int>(type: "int", nullable: true),
                    GroupID = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.WordId);
                    table.ForeignKey(
                        name: "FK_Words_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Words_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Words_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Words_Words_RootID",
                        column: x => x.RootID,
                        principalTable: "Words",
                        principalColumn: "WordId");
                });

            migrationBuilder.CreateTable(
                name: "DictionaryReferenceWords",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DictionaryID = table.Column<int>(type: "int", nullable: false),
                    WordID = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<int>(type: "int", nullable: false),
                    Column = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DictionaryReferenceWords", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DictionaryReferenceWords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DictionaryReferenceWords_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DictionaryReferenceWords_Dictionaries_DictionaryID",
                        column: x => x.DictionaryID,
                        principalTable: "Dictionaries",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DictionaryReferenceWords_Words_WordID",
                        column: x => x.WordID,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordExplanations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Explanation = table.Column<string>(type: "nvarchar(1500)", nullable: false),
                    ExplanationNormalized = table.Column<string>(type: "nvarchar(1500)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WordID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordExplanations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WordExplanations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WordExplanations_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WordExplanations_Words_WordID",
                        column: x => x.WordID,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordMeanings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WordID = table.Column<int>(type: "int", nullable: false),
                    MeaningID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordMeanings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WordMeanings_Meanings_MeaningID",
                        column: x => x.MeaningID,
                        principalTable: "Meanings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordMeanings_Words_WordID",
                        column: x => x.WordID,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Examples",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExampleText = table.Column<string>(type: "nvarchar(1500)", nullable: false),
                    ExampleTextNormalized = table.Column<string>(type: "nvarchar(1500)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pronunciation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WordMeaningID = table.Column<int>(type: "int", nullable: true),
                    ParentExampleID = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examples", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Examples_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Examples_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Examples_Examples_ParentExampleID",
                        column: x => x.ParentExampleID,
                        principalTable: "Examples",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Examples_WordMeanings_WordMeaningID",
                        column: x => x.WordMeaningID,
                        principalTable: "WordMeanings",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "WordMeaningBibles",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WordMeaningID = table.Column<int>(type: "int", nullable: false),
                    BibleID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordMeaningBibles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WordMeaningBibles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WordMeaningBibles_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WordMeaningBibles_Bibles_BibleID",
                        column: x => x.BibleID,
                        principalTable: "Bibles",
                        principalColumn: "BibleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordMeaningBibles_WordMeanings_WordMeaningID",
                        column: x => x.WordMeaningID,
                        principalTable: "WordMeanings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "64d567f7-bbf4-4fc2-91a5-9cccab272a9d", "USER", "User", "USER" },
                    { "7aa33766-73fb-4d24-955b-c744d7ca56c8", "ADMIN", "Admin", "ADMIN" },
                    { "7ee2f100-f6f3-4bd5-ba06-6874194dae7d", "STUDENT", "Student", "STUDENT" },
                    { "e9aaf9ab-58ef-4b2d-9648-857bc31c41a8", "INSTRUCTOR", "Instructor", "INSTRUCTOR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bible_TextNormalized",
                table: "Bibles",
                column: "TextNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Bibles_Book_Chapter_Verse_Language_Edition",
                table: "Bibles",
                columns: new[] { "Book", "Chapter", "Verse", "Language", "Edition" });

            migrationBuilder.CreateIndex(
                name: "IX_Bibles_CreatedByUserId",
                table: "Bibles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bibles_ModifiedByUserId",
                table: "Bibles",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bibles_Text",
                table: "Bibles",
                column: "Text");

            migrationBuilder.CreateIndex(
                name: "IX_Dictionaries_CreatedByUserId",
                table: "Dictionaries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dictionaries_ModifiedByUserId",
                table: "Dictionaries",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DictionaryReferenceWords_CreatedByUserId",
                table: "DictionaryReferenceWords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DictionaryReferenceWords_DictionaryID",
                table: "DictionaryReferenceWords",
                column: "DictionaryID");

            migrationBuilder.CreateIndex(
                name: "IX_DictionaryReferenceWords_ModifiedByUserId",
                table: "DictionaryReferenceWords",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DictionaryReferenceWords_WordID",
                table: "DictionaryReferenceWords",
                column: "WordID");

            migrationBuilder.CreateIndex(
                name: "IX_Example_ExampleTextNormalized",
                table: "Examples",
                column: "ExampleTextNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_CreatedByUserId",
                table: "Examples",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_ExampleText",
                table: "Examples",
                column: "ExampleText");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_ModifiedByUserId",
                table: "Examples",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_ParentExampleID",
                table: "Examples",
                column: "ParentExampleID");

            migrationBuilder.CreateIndex(
                name: "IX_Examples_WordMeaningID",
                table: "Examples",
                column: "WordMeaningID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupExplanation_ExplanationNormalized",
                table: "GroupExplanations",
                column: "ExplanationNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_GroupExplanations_CreatedByUserId",
                table: "GroupExplanations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupExplanations_Explanation",
                table: "GroupExplanations",
                column: "Explanation");

            migrationBuilder.CreateIndex(
                name: "IX_GroupExplanations_GroupID",
                table: "GroupExplanations",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupExplanations_ModifiedByUserId",
                table: "GroupExplanations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRelations_CreatedByUserId",
                table: "GroupRelations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRelations_ModifiedByUserId",
                table: "GroupRelations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRelations_ParentGroupID",
                table: "GroupRelations",
                column: "ParentGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRelations_RelatedGroupID",
                table: "GroupRelations",
                column: "RelatedGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatedByUserId",
                table: "Groups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ModifiedByUserId",
                table: "Groups",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                table: "Groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GroupWord_NameNormalized",
                table: "Groups",
                column: "NameNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Meaning_MeaningTextNormalized",
                table: "Meanings",
                column: "MeaningTextNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Meanings_CreatedByUserId",
                table: "Meanings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meanings_MeaningText",
                table: "Meanings",
                column: "MeaningText");

            migrationBuilder.CreateIndex(
                name: "IX_Meanings_ModifiedByUserId",
                table: "Meanings",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meanings_ParentMeaningID",
                table: "Meanings",
                column: "ParentMeaningID");

            migrationBuilder.CreateIndex(
                name: "IX_WordExplanation_ExplanationNormalized",
                table: "WordExplanations",
                column: "ExplanationNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_WordExplanations_CreatedByUserId",
                table: "WordExplanations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordExplanations_Explanation",
                table: "WordExplanations",
                column: "Explanation");

            migrationBuilder.CreateIndex(
                name: "IX_WordExplanations_ModifiedByUserId",
                table: "WordExplanations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordExplanations_WordID",
                table: "WordExplanations",
                column: "WordID");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeaningBibles_BibleID",
                table: "WordMeaningBibles",
                column: "BibleID");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeaningBibles_CreatedByUserId",
                table: "WordMeaningBibles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeaningBibles_ModifiedByUserId",
                table: "WordMeaningBibles",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeaningBibles_WordMeaningID",
                table: "WordMeaningBibles",
                column: "WordMeaningID");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeanings_MeaningID",
                table: "WordMeanings",
                column: "MeaningID");

            migrationBuilder.CreateIndex(
                name: "IX_WordMeanings_WordID",
                table: "WordMeanings",
                column: "WordID");

            migrationBuilder.CreateIndex(
                name: "IX_Word_Word_textNormalized",
                table: "Words",
                column: "Word_textNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Words_CreatedByUserId",
                table: "Words",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Words_GroupID",
                table: "Words",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Words_ModifiedByUserId",
                table: "Words",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Words_RootID",
                table: "Words",
                column: "RootID");

            migrationBuilder.CreateIndex(
                name: "IX_Words_Word_text",
                table: "Words",
                column: "Word_text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DictionaryReferenceWords");

            migrationBuilder.DropTable(
                name: "Examples");

            migrationBuilder.DropTable(
                name: "GroupExplanations");

            migrationBuilder.DropTable(
                name: "GroupRelations");

            migrationBuilder.DropTable(
                name: "WordExplanations");

            migrationBuilder.DropTable(
                name: "WordMeaningBibles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Dictionaries");

            migrationBuilder.DropTable(
                name: "Bibles");

            migrationBuilder.DropTable(
                name: "WordMeanings");

            migrationBuilder.DropTable(
                name: "Meanings");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
