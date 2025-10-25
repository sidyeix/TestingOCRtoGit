using UnityEngine;

[System.Serializable]
public class IngredientData
{
    public string ingredient; // Name of the scanned ingredient
    public int rarity;        // Rarity level (0=Common, 1=Rare, 2=Ultra Rare)
    public string status;     // Success or error status
    
    // NEW FIELDS for enhanced system - MUST MATCH JSON FIELD NAMES EXACTLY
    public string fingerprint;      // Unique product identifier
    public int total_detected;      // How many ingredients were detected
    public string rarity_breakdown; // Rarity distribution (format: "C:2,R:1,UR:0")
    
    // Check if the data is valid and usable
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ingredient) && !string.IsNullOrEmpty(status);
    }
    
    // Check if this is a duplicate product scan
    public bool IsDuplicateProduct()
    {
        return !string.IsNullOrEmpty(fingerprint) && 
               ProductManager.IsProductAlreadyScanned(fingerprint);
    }
    
    // Convert rarity number to readable name
    public string GetRarityName()
    {
        switch (rarity)
        {
            case 0: return "Common";
            case 1: return "Rare";
            case 2: return "Ultra Rare";
            default: return "Unknown";
        }
    }
    
    // Get color based on rarity for UI styling
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case 0: return Color.white;    // Common - White
            case 1: return Color.blue;     // Rare - Blue
            case 2: return Color.magenta;  // Ultra Rare - Magenta/Purple
            default: return Color.gray;    // Unknown - Gray
        }
    }
    
    // Convenience property for cleaner code access
    public int totalDetected { get { return total_detected; } }
    public string rarityBreakdown { get { return rarity_breakdown; } }
}