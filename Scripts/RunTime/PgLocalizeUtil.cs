
using UnityEngine.Localization.Settings;

public static class PgLocalizeUtil
{
    public static string GetLocalizeString(string collection, string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable(collection);
        return table.GetEntry(key)?.LocalizedValue;
    }

    public static string GetLocalizeString(string key)
    {
        var data = key.Split('_');
        return GetLocalizeString(data[0], key);
    }
}

