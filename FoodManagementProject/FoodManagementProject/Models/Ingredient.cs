using System;
using System.Collections.Generic;

namespace FoodManagementProject.Models;

public partial class Ingredient
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = null!;

    public string NormalisedName { get; set; } = null!;

    public string Category { get; set; } = null!;
}
