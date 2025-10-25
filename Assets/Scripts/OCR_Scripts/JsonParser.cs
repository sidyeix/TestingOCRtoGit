using UnityEngine;
using System;

public static class JsonParser
{
    // Main method to parse plugin response - handles both success and error cases
    public static IngredientData ParseIngredientResponse(string jsonResponse)
    {
        // First check if it's an error message (starts with "ERROR")
        if (jsonResponse.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Plugin error: " + jsonResponse);
            return CreateErrorIngredient(jsonResponse);
        }
        
        try
        {
            // Try to parse the JSON string into IngredientData object
            IngredientData data = JsonUtility.FromJson<IngredientData>(jsonResponse);
            
            // Validate the parsed data
            if (data != null && data.IsValid())
            {
                Debug.Log($"Successfully parsed ingredient: {data.ingredient} " +
                         $"(Total detected: {data.total_detected}, Fingerprint: {data.fingerprint}, " +
                         $"Rarity breakdown: {data.rarity_breakdown})");
                return data;
            }
            else
            {
                Debug.LogWarning("Invalid JSON data received: " + jsonResponse);
                return CreateErrorIngredient("Invalid ingredient data format");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("JSON parse error: " + e.Message + "\nRaw response: " + jsonResponse);
            return CreateErrorIngredient("Parse error: " + e.Message);
        }
    }
    
    // Helper method to create error ingredient data
    private static IngredientData CreateErrorIngredient(string errorMessage)
    {
        return new IngredientData
        {
            ingredient = "Error",
            rarity = -1,
            status = errorMessage,
            fingerprint = "",
            total_detected = 0,
            rarity_breakdown = ""
        };
    }
}