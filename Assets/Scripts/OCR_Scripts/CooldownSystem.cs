using UnityEngine;
using System;
using System.Collections.Generic;

// SIMPLIFIED Cooldown System - Only handles global scan cooldown
// Product-based limits are now handled by ProductManager
public static class CooldownSystem
{
    private static DateTime lastGlobalScanTime = DateTime.MinValue;

    // Global 10-second cooldown between any scans
    public static bool CanScanAnyIngredient()
    {
        return (DateTime.Now - lastGlobalScanTime).TotalSeconds >= 10f;
    }

    // Check if a specific ingredient can be scanned right now
    // NOW SIMPLIFIED: Only checks global cooldown, no ingredient restrictions
    public static bool CanScanIngredient(string ingredientName)
    {
        return CanScanAnyIngredient(); // No more ingredient-specific restrictions
    }

    // Ingredient max limits are removed - products have limits instead
    public static bool IsIngredientMaxedOut(string ingredientName)
    {
        return false; // No ingredient can be "maxed out" anymore
    }

    // Record that a scan occurred (only updates global timestamp)
    public static void RecordScan(string ingredientName)
    {
        lastGlobalScanTime = DateTime.Now;
        Debug.Log($"Global scan recorded at: {lastGlobalScanTime}");
    }

    // Get how many times an ingredient has been scanned (not used anymore)
    public static int GetScanCount(string ingredientName)
    {
        return 0; // This data is no longer tracked per ingredient
    }

    // Get remaining time on global 10-second cooldown
    public static TimeSpan GetGlobalCooldown()
    {
        DateTime nextScanTime = lastGlobalScanTime.AddSeconds(10);
        TimeSpan remaining = nextScanTime - DateTime.Now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    // Get readable status message (simplified)
    public static string GetCooldownStatus(string ingredientName)
    {
        TimeSpan globalCooldown = GetGlobalCooldown();
        if (globalCooldown.TotalSeconds > 0)
        {
            return $"Scan cooldown: {globalCooldown:ss}s remaining";
        }
        return "Ready to scan";
    }

    // All ingredients are always "available" now (products have limits instead)
    public static bool AreAnyIngredientsAvailable()
    {
        return true; // Infinite ingredients available
    }

    // All ingredients are available (no limits per ingredient)
    public static int GetAvailableIngredientsCount()
    {
        return 999; // Show a large number to indicate unlimited
    }

    // Get total number of unique ingredients discovered
    // This now uses ProductManager since we track products, not ingredients
    public static int GetTotalDiscoveredIngredients()
    {
        return ProductManager.GetTotalScannedProducts();
    }

    // Get number of maxed-out ingredients (not applicable anymore)
    public static int GetMaxedOutIngredientsCount()
    {
        return 0; // No ingredients are maxed out
    }

}

