
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class PgLocalizeUtil
{
    public static string GetLocalizeStringByTableAndKey(string collection, string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable(collection);
        var entry = table.GetEntry(key);
        var result = LocalizationSettings.StringDatabase.GenerateLocalizedString(table, entry, null, null, null, args);
        return result;
    }

    public static string GetLocalizeString(string key, params object[] args)
    {
        //var data = key.Split('_');
        return GetLocalizeStringByTableAndKey(LocalizationSettings.StringDatabase.DefaultTable, key, args);
    }

    public static void LoadLocale(string languageIdentifier)
    {
        LocalizationSettings settings = LocalizationSettings.Instance;
        LocaleIdentifier localeCode = new LocaleIdentifier(languageIdentifier);//can be "en" "de" "ja" etc.
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            Locale aLocale = LocalizationSettings.AvailableLocales.Locales[i];
            LocaleIdentifier anIdentifier = aLocale.Identifier;
            if (anIdentifier == localeCode)
            {
                LocalizationSettings.SelectedLocale = aLocale;
            }
        }
    }

    public static void AddLocaleChangedCallback(System.Action<Locale> callback)
    {
        LocalizationSettings.SelectedLocaleChanged += callback;
    }
}

