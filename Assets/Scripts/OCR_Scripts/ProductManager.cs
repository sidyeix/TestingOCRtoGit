using UnityEngine;
using System;
using System.Collections.Generic;

// Manages product-based scanning limits with real-time countdown
public static class ProductManager
{
    private static Dictionary<string, ProductScanData> scannedProducts = new Dictionary<string, ProductScanData>();
    private static DateTime lastProductResetTime = DateTime.Now;
    
    // Event to notify when cooldowns update
    public static event Action<string, TimeSpan> OnCooldownUpdated;
    public static event Action OnProductsUpdated;
    
    [System.Serializable]
    public class ProductScanData
    {
        public string fingerprint;
        public DateTime firstScanTime;
        public int scanCount;
        public string lastSelectedIngredient;
        
        public ProductScanData(string fingerprint)
        {
            this.fingerprint = fingerprint;
            this.firstScanTime = DateTime.Now;
            this.scanCount = 1;
        }
        
        public bool CanScanAgain()
        {
            return scanCount < 3;
        }
        
        public void RecordScan()
        {
            scanCount++;
        }
        
        public TimeSpan GetRemainingCooldown()
        {
            if (scanCount >= 3)
            {
                DateTime cooldownEnd = firstScanTime.AddHours(24);
                TimeSpan remaining = cooldownEnd - DateTime.Now;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
        
        public bool IsCooldownActive()
        {
            return scanCount >= 3 && GetRemainingCooldown().TotalSeconds > 0;
        }
    }
    
    // Check if a product has been scanned too many times
    public static bool IsProductAlreadyScanned(string fingerprint)
    {
        CleanOldProducts();
        
        if (scannedProducts.ContainsKey(fingerprint))
        {
            ProductScanData data = scannedProducts[fingerprint];
            return !data.CanScanAgain();
        }
        return false;
    }
    
    // Record a product scan
    public static void RecordProductScan(string fingerprint, string selectedIngredient)
    {
        CleanOldProducts();
        
        if (scannedProducts.ContainsKey(fingerprint))
        {
            ProductScanData data = scannedProducts[fingerprint];
            data.RecordScan();
            data.lastSelectedIngredient = selectedIngredient;
        }
        else
        {
            scannedProducts[fingerprint] = new ProductScanData(fingerprint);
            scannedProducts[fingerprint].lastSelectedIngredient = selectedIngredient;
        }
        
        Debug.Log($"Product scanned: {fingerprint}. Total scans: {scannedProducts[fingerprint].scanCount}/3");
        OnProductsUpdated?.Invoke();
    }
    
    // Get how many times a product has been scanned
    public static int GetProductScanCount(string fingerprint)
    {
        if (scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].scanCount;
        }
        return 0;
    }
    
    // Get remaining time until product can be scanned again
    public static TimeSpan GetProductCooldown(string fingerprint)
    {
        if (scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].GetRemainingCooldown();
        }
        return TimeSpan.Zero;
    }
    
    // Check if any products are currently on cooldown
    public static bool AnyProductsOnCooldown()
    {
        foreach (var product in scannedProducts.Values)
        {
            if (product.IsCooldownActive())
            {
                return true;
            }
        }
        return false;
    }
    
    // Get all products currently on cooldown
    public static List<string> GetProductsOnCooldown()
    {
        List<string> cooldownProducts = new List<string>();
        foreach (var pair in scannedProducts)
        {
            if (pair.Value.IsCooldownActive())
            {
                cooldownProducts.Add(pair.Key);
            }
        }
        return cooldownProducts;
    }
    
    // Update cooldowns and trigger events
    public static void UpdateCooldowns()
    {
        bool changed = CleanOldProducts(); // Check for expired cooldowns
        
        // Notify about updated cooldowns
        foreach (var pair in scannedProducts)
        {
            if (pair.Value.IsCooldownActive())
            {
                TimeSpan remaining = pair.Value.GetRemainingCooldown();
                OnCooldownUpdated?.Invoke(pair.Key, remaining);
                
                // If cooldown just expired, trigger update
                if (remaining.TotalSeconds <= 0)
                {
                    changed = true;
                }
            }
        }
        
        if (changed)
        {
            OnProductsUpdated?.Invoke();
        }
    }
    
    // Auto-clean products older than 24 hours
    private static bool CleanOldProducts()
    {
        bool removedAny = false;
        List<string> productsToRemove = new List<string>();
        
        foreach (var pair in scannedProducts)
        {
            if ((DateTime.Now - pair.Value.firstScanTime).TotalHours >= 24)
            {
                productsToRemove.Add(pair.Key);
                removedAny = true;
            }
        }
        
        foreach (string fingerprint in productsToRemove)
        {
            scannedProducts.Remove(fingerprint);
            Debug.Log($"Removed expired product: {fingerprint}");
        }
        
        // Update reset time if we cleaned anything
        if (removedAny)
        {
            lastProductResetTime = DateTime.Now;
        }
        
        return removedAny;
    }
    
    // Get total number of unique products scanned
    public static int GetTotalScannedProducts()
    {
        return scannedProducts.Count;
    }
    
    // Get scan data for a specific product
    public static ProductScanData GetProductData(string fingerprint)
    {
        if (scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint];
        }
        return null;
    }
}