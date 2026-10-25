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
    // to be added
    // public virtual DbSet<HouseholdMember> HouseholdMembers { get; set; }
    // public virtual DbSet<AppUser> AppUsers { get; set; }

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
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
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
}


}
