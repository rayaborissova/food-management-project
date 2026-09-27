using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Models;

public partial class IngredientsDbV1Context : DbContext
{
    public IngredientsDbV1Context(DbContextOptions<IngredientsDbV1Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ingredients_pkey");

            entity.ToTable("ingredients");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.NormalisedName).HasColumnName("normalised_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
