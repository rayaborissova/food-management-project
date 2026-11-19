using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Models;

public partial class FoodManagementDbContext : DbContext
{
    public FoodManagementDbContext(DbContextOptions<FoodManagementDbContext> options)
        : base(options)
    {
    }

    // setting dbset properties
    public virtual DbSet<Ingredient> Ingredients { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Unit> Units { get; set; }
    public virtual DbSet<Household> Households { get; set; }
    public virtual DbSet<InventoryBatch> InventoryBatches { get; set; }
    public virtual DbSet<ShoppingListItem> ShoppingListItems { get; set; }
    public virtual DbSet<HouseholdMember> HouseholdMembers { get; set; }
    public virtual DbSet<AppUser> AppUsers { get; set; }
    public virtual DbSet<HouseholdInvite> HouseholdInvites { get; set; } //for household invitations
    public virtual DbSet<IngredientRequest> IngredientRequests { get; set; }
    public virtual DbSet<IngredientReport> IngredientReports { get; set; }
    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //mapping the category table columns
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("category");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName).HasColumnName("category_name");
        });

        //mapping the ingredient table columns 
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredient");
            entity.HasKey(e => e.IngredientId);
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IngredientName).HasColumnName("ingredient_name");
            entity.Property(e => e.IngredientNameNormalized).HasColumnName("ingredient_name_normalized");
            entity.Property(e => e.IngredientSource).HasColumnName("ingredient_source");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.HasOne(e => e.Category).WithMany(c => c.Ingredients).HasForeignKey(e => e.CategoryId); // stating ingredient relationship to category
        });

        //mapping the unit table columns
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("unit");
            entity.HasKey(e => e.UnitId);
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitName).HasColumnName("unit_name");
            entity.Property(e => e.Abbreviation).HasColumnName("abbreviation");
        });

        //mapping the household table columns
        modelBuilder.Entity<Household>(entity =>
        {
            entity.ToTable("household");
            entity.HasKey(e => e.HouseholdId);
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");
            entity.Property(e => e.HouseholdName).HasColumnName("household_name");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.PersonalUserId).HasColumnName("personal_user_id");
        });

        //mapping the ingredients batch table columns
        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.ToTable("inventory_batch");
            entity.HasKey(e => e.IngredientBatchId);
            entity.Property(e => e.IngredientBatchId).HasColumnName("ingredient_batch_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.DateAdded).HasColumnName("date_added");
            entity.HasOne(e => e.Ingredient).WithMany(c => c.InventoryBatches).HasForeignKey(e => e.IngredientId); // stating inventory batch relationships
            entity.HasOne(e => e.Household).WithMany(c => c.InventoryBatches).HasForeignKey(e => e.HouseholdId);
            entity.HasOne(e => e.Unit).WithMany(c => c.InventoryBatches).HasForeignKey(e => e.UnitId);
        });
        //mapping the shopping list item table columns
        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.ToTable("shopping_list_item");
            entity.HasKey(e => e.ShoppingListItemId);
            entity.Property(e => e.ShoppingListItemId).HasColumnName("shoppinglist_item_id");
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.IsPurchased).HasColumnName("is_purchased");
            entity.Property(e => e.ItemSource).HasColumnName("item_source");
            entity.Property(e => e.DateAdded).HasColumnName("date_added").HasColumnType("timestamp without time zone");
            entity.HasOne(e => e.Ingredient).WithMany(c => c.ShoppingListItems).HasForeignKey(e => e.IngredientId);
            entity.HasOne(e => e.Household).WithMany(c => c.ShoppingListItems).HasForeignKey(e => e.HouseholdId);
            entity.HasOne(e => e.Unit).WithMany(c => c.ShoppingListItems).HasForeignKey(e => e.UnitId);
        });

        //mapping the app user table columns
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_user");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.UserRole).HasColumnName("user_role");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        //mapping the household member table columns
        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.ToTable("household_member");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");
            entity.Property(e => e.HouseholdRole).HasColumnName("household_role");
            entity.Property(e => e.DateJoined).HasColumnName("date_joined").HasColumnType("timestamp without time zone");
            entity.HasOne(e => e.User).WithOne(u => u.Membership).HasForeignKey<HouseholdMember>(e => e.UserId);
            entity.HasOne(e => e.Household).WithMany(h => h.Members).HasForeignKey(e => e.HouseholdId);
        });

        //mapping the household invite table columns
        modelBuilder.Entity<HouseholdInvite>(entity =>
        {
            entity.ToTable("household_invite");
            entity.HasKey(e => e.InviteId);
            entity.Property(e => e.InviteId).HasColumnName("invite_id");
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp without time zone");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.Household).WithMany().HasForeignKey(e => e.HouseholdId);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.CreatedBy);
        });

        //mapping the ingredient request table columns
        modelBuilder.Entity<IngredientRequest>(entity =>
        {
            entity.ToTable("ingredient_request");
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.RequestedName).HasColumnName("requested_name");
            entity.Property(e => e.SuggestedCategoryId).HasColumnName("suggested_category_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.RequestStatus).HasColumnName("request_status");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.DateReviewed).HasColumnName("date_reviewed").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ReviewNote).HasColumnName("review_note");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.HasOne(e => e.Requester).WithMany().HasForeignKey(e => e.RequestedBy);
            entity.HasOne(e => e.Reviewer).WithMany().HasForeignKey(e => e.ReviewedBy);
            entity.HasOne(e => e.SuggestedCategory).WithMany().HasForeignKey(e => e.SuggestedCategoryId);
            entity.HasOne(e => e.Ingredient).WithMany().HasForeignKey(e => e.IngredientId);
        });

        //mapping to the ingredient reports
        modelBuilder.Entity<IngredientReport>(entity =>
        {
            entity.ToTable("ingredient_report");
            entity.HasKey(e => e.ReportId);
            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.ReportedBy).HasColumnName("reported_by");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.SuggestedCategoryId).HasColumnName("suggested_category_id");
            entity.Property(e => e.ReportStatus).HasColumnName("report_status");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.DateReviewed).HasColumnName("date_reviewed").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ReviewNote).HasColumnName("review_note");
            entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
            entity.Property(e => e.DateResolved).HasColumnName("date_resolved").HasColumnType("timestamp without time zone");
            entity.Property(e => e.Resolution).HasColumnName("resolution");
            entity.Property(e => e.ResolutionNote).HasColumnName("resolution_note");
            entity.HasOne(e => e.Ingredient).WithMany().HasForeignKey(e => e.IngredientId);
            entity.HasOne(e => e.Reporter).WithMany().HasForeignKey(e => e.ReportedBy);
            entity.HasOne(e => e.Reviewer).WithMany().HasForeignKey(e => e.ReviewedBy);
            entity.HasOne(e => e.Resolver).WithMany().HasForeignKey(e => e.ResolvedBy);
            entity.HasOne(e => e.SuggestedCategory).WithMany().HasForeignKey(e => e.SuggestedCategoryId);
        });

        //mapping to the password reset table
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("password_reset");
            entity.HasKey(e => e.ResetId);
            entity.Property(e => e.ResetId).HasColumnName("reset_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp without time zone");
            entity.Property(e => e.UsedAt).HasColumnName("used_at").HasColumnType("timestamp without time zone");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });

    }


}
