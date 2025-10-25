using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IngredientDatabase", menuName = "NutriVenture/Ingredient Database")]
public class IngredientDatabase : ScriptableObject
{
    [System.Serializable]
    public class IngredientInfo
    {
        public string ingredientName;
        public GameObject modelPrefab;
    }
    
    public List<IngredientInfo> ingredients = new List<IngredientInfo>();
    
    public IngredientInfo GetIngredientInfo(string name)
    {
        return ingredients.Find(i => i.ingredientName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
}