using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

public class LocaleDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;


    private void Start()
    {
      
    }

    private void PopulateLocaleDropdown()
    {
        if (dropdown == null)
        {
            Debug.LogError("Dropdown reference is null!");
            return;
        }

        // Get all available cultures
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        // Create a list to store locale codes and names
        List<string> localeOptions = new List<string>();
        List<string> localeCodes = new List<string>();

        // Convert cultures to locale format and filter out empty names
        foreach (CultureInfo culture in cultures)
        {
            if (!string.IsNullOrEmpty(culture.Name) && culture.Name.Length >= 2 && !culture.Name.ToLower().Contains("-"))
            {
                string localeCode = culture.Name.ToLower();
                string displayName = $"{localeCode} - {culture.DisplayName}";

                localeOptions.Add(displayName);
                localeCodes.Add(localeCode);
            }
        }

        // Remove duplicates and sort
        var uniqueLocales = localeOptions.Zip(localeCodes, (option, code) => new { Option = option, Code = code })
                                       .GroupBy(x => x.Code)
                                       .Select(g => g.First())
                                       .OrderBy(x => x.Code)
                                       .ToList();

        // Create final lists with priority ordering
        List<string> finalOptions = new List<string>();
        List<string> finalCodes = new List<string>();

        // Add "en" first if it exists
        var enLocale = uniqueLocales.FirstOrDefault(x => x.Code == "en");
        if (enLocale != null)
        {
            finalOptions.Add(enLocale.Option);
            finalCodes.Add(enLocale.Code);
        }

        // Add "id" second if it exists
        var idLocale = uniqueLocales.FirstOrDefault(x => x.Code == "id");
        if (idLocale != null)
        {
            finalOptions.Add(idLocale.Option);
            finalCodes.Add(idLocale.Code);
        }

        // Add all other locales
        foreach (var locale in uniqueLocales)
        {
            if (locale.Code != "en" && locale.Code != "id")
            {
                finalOptions.Add(locale.Option);
                finalCodes.Add(locale.Code);
            }
        }

        // Clear existing options and add new ones
        dropdown.ClearOptions();
        dropdown.AddOptions(finalOptions);

        // Store locale codes for later use (optional)
        StoreLocaleCodes(finalCodes);

        Debug.Log($"Populated dropdown with {finalOptions.Count} locales");
    }

    // Optional: Store locale codes to access them later
    private List<string> storedLocaleCodes = new List<string>();

    private void StoreLocaleCodes(List<string> codes)
    {
        storedLocaleCodes = codes;
    }

    // Get the currently selected locale code
    public string GetSelectedLocaleCode()
    {
        if (dropdown != null && storedLocaleCodes.Count > dropdown.value)
        {
            return storedLocaleCodes[dropdown.value];
        }
        return "en"; // default fallback
    }

    public void setLocale(int index)
    {
        PopulateLocaleDropdown();
        dropdown.value = index;
        dropdown.RefreshShownValue();
    }
}