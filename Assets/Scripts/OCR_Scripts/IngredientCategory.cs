using UnityEngine;

public static class IngredientCategory
{
    public static string GetCategory(string ingredient)
    {
        string lowerIngredient = ingredient.ToLower();

        // PRESERVATIVES - Match ALL preservatives from Java list
        if (lowerIngredient.Contains("sorbate") || lowerIngredient.Contains("benzoate") ||
            lowerIngredient.Contains("propionate") || lowerIngredient.Contains("nitrite") ||
            lowerIngredient.Contains("sulfite") || lowerIngredient.Contains("tbhq") ||
            lowerIngredient.Contains("bha") || lowerIngredient.Contains("bht") ||
            lowerIngredient.Contains("natamycin") || lowerIngredient.Contains("preservative"))
            return "PRESERVATIVE";

        // SWEETENERS - Match ALL sweeteners from Java list
        if (lowerIngredient.Contains("fructose") || lowerIngredient.Contains("aspartame") ||
            lowerIngredient.Contains("sucralose") || lowerIngredient.Contains("acesulfame") ||
            lowerIngredient.Contains("sorbitol") || lowerIngredient.Contains("maltodextrin") ||
            lowerIngredient.Contains("stevia") || lowerIngredient.Contains("monk fruit") ||
            lowerIngredient.Contains("sweetener") || lowerIngredient.Contains("syrup"))
            return "SWEETENER";

        // FORTIFICANTS - Match ALL fortificants from Java list  
        if (lowerIngredient.Contains("ascorbic") || lowerIngredient.Contains("niacin") ||
            lowerIngredient.Contains("ferrous") || lowerIngredient.Contains("zinc") ||
            lowerIngredient.Contains("pantothenate") || lowerIngredient.Contains("pyridoxine") ||
            lowerIngredient.Contains("cholecalciferol") || lowerIngredient.Contains("cyanocobalamin") ||
            lowerIngredient.Contains("vitamin") || lowerIngredient.Contains("fortified"))
            return "FORTIFICANT";

        // ALLERGENS - Match ALL allergens from Java list
        if (lowerIngredient.Contains("soy") || lowerIngredient.Contains("whey") ||
            lowerIngredient.Contains("egg") || lowerIngredient.Contains("milk") ||
            lowerIngredient.Contains("caseinate") || lowerIngredient.Contains("caseinates") || // Added plural
            lowerIngredient.Contains("wheat") || lowerIngredient.Contains("gluten") ||
            lowerIngredient.Contains("lupin") || lowerIngredient.Contains("sesame") ||
            lowerIngredient.Contains("protein") || lowerIngredient.Contains("allergen"))
            return "ALLERGEN";

        return "OTHER";
    }

    public static Color GetCategoryColor(string category)
    {
        switch (category)
        {
            case "PRESERVATIVE": return new Color(1f, 0.8f, 0.8f); // Light red
            case "SWEETENER": return new Color(1f, 1f, 0.8f);      // Light yellow
            case "FORTIFICANT": return new Color(0.8f, 0.9f, 1f);  // Light blue
            case "ALLERGEN": return new Color(1f, 0.9f, 0.8f);     // Light orange
            default: return Color.white;
        }
    }
}