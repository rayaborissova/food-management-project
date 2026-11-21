using FoodManagementProject.Models;

namespace FoodManagementProject.Services
{
    public static class InventoryViewBuilder
    {
        public const int ExpiringSoonDays = 3; // a batch expiring within this many days is considered "soon"
        private const int NoDateSortDays = 99999; // sort value for ingredients without any expiry date, they go last

        public static InventoryIndexViewModel Build(IEnumerable<InventoryBatch> batches, DateOnly today)
        {
            var groups = batches.GroupBy(b => b.IngredientId).Select(g => BuildGroup(g.ToList(), today)).OrderBy(g => g.SortDays).ThenBy(g => g.IngredientName).ToList();

            var tabs = groups.GroupBy(g => g.CategoryId).Select(g => new InventoryCategoryTab { Id = g.Key, Name = g.First().CategoryName, Count = g.Count() }).OrderBy(t => t.Name).ToList();

            return new InventoryIndexViewModel
            {
                Groups = groups,
                Categories = tabs,
                ExpireCount = groups.Count(g => g.Status == "expired"),
                SoonCount = groups.Count(g => g.Status == "soon"),
                NoDateCount = groups.Count(g => g.Status == "none")
            };
        }

        private static InventoryGroupViewModel BuildGroup(List<InventoryBatch> batches, DateOnly today)
        {
            var first = batches[0];
            var rows = batches.OrderBy(b => b.ExpirationDate ?? DateOnly.MaxValue).Select(b => new InventoryBatchRowViewModel
            {
                IngredientBatchId = b.IngredientBatchId,
                QuantityText = QuantityTextFor(b.Quantity, b.Unit),
                ExpiryText = ExpiryTextFor(b.ExpirationDate, today),
                Status = StatusFor(b.ExpirationDate, today)
            }).ToList();

            // ingredient takes the worst status among its batches
            var status = rows.Any(r => r.Status == "expired") ? "expired"
                        : rows.Any(r => r.Status == "soon") ? "soon"
                        : rows.Any(r => r.Status == "ok") ? "ok"
                        : "none";

            var dated = batches.Where(b => b.ExpirationDate.HasValue).Select(b => b.ExpirationDate.Value).ToList();
            DateOnly? earliest = dated.Count > 0 ? dated.Min() : null;

            // total sum is shown only when every batch has a quantity in the same unit
            var amountText = string.Empty;
            if (batches.Count == 1)
            {
                amountText = first.Quantity.HasValue ? QuantityTextFor(first.Quantity, first.Unit) : string.Empty;
            } else if (batches.All(b => b.Quantity.HasValue && b.UnitId.HasValue && b.UnitId == first.UnitId)) 
            {
                amountText = $"{batches.Sum(b => b.Quantity!.Value):0.##} {first.Unit?.Abbreviation} total";
            }

            var batchText = batches.Count > 1 ? $"{batches.Count} batches" : string.Empty;
            var categoryName = first.Ingredient.Category?.CategoryName ?? "Other";
            var name = first.Ingredient.IngredientName;

            return new InventoryGroupViewModel
            {
                IngredientId = first.IngredientId,
                IngredientName = name,
                Initial = name.Length > 0 ? char.ToUpperInvariant(name[0]).ToString() : "?",
                CategoryId = first.Ingredient.CategoryId,
                CategoryName = categoryName,
                MetaText = string.Join(" · ", new[] { categoryName, batchText, amountText }.Where(s => s.Length > 0)),
                Status = status,
                ExpiryText = ExpiryTextFor(earliest, today),
                SortDays = earliest.HasValue ? earliest.Value.DayNumber - today.DayNumber : NoDateSortDays,
                Batches = rows
            };

        }


        private static string StatusFor(DateOnly? date, DateOnly today)
        {
            if (date == null)
            {
                return "none";
            }
            var days = date.Value.DayNumber - today.DayNumber;
            return days < 0 ? "expired" : days <= ExpiringSoonDays ? "soon" : "ok";
        }

        private static string ExpiryTextFor(DateOnly? date, DateOnly today)
        {
            if (date == null)
            {
                return "No date";
            }
            var days = date.Value.DayNumber - today.DayNumber;
            return days switch
            {
                < -1 => $"Expired {-days} days ago.",
                -1 => $"Expired yesterday.",
                0 => $"Expires today.",
                1 => $"Expires tomorrow.",
                <= 30 => $"Expires in {days} days.",
                _ => $"Expires {date.Value:dd/MM/yyyy}"
            };
        }

        private static string QuantityTextFor(decimal? quantity, Unit? unit)
        {
            if (quantity == null)
            {
                return "Amount not specified";
            }
            var amount = quantity.Value.ToString("0.##");
            return unit == null ? amount : $"{amount} {unit.Abbreviation}";
        }

    }
}
