using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class PgLocalizeStringEvent : LocalizeStringEvent
{
    string originCollection;

    private void Awake()
    {
        originCollection = StringReference.TableReference;
    }

    public void SetArgs(params object[] args)
    {
        StringReference.Arguments = args;
        RefreshString();
    }

    public void SetString(string key, params object[] args)
    {
        //bool isKeyExist = CheckCollectionHasKey(key);
        StringReference.Arguments = args;
        //if (isKeyExist)
        //{
            StringReference.SetReference(originCollection, key);
        //}
        //else
        //{
        //    StringReference.SetReference(LocalizationSettings.StringDatabase.DefaultTable, key);
        //}
    }

    public bool CheckCollectionHasKey(string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable(originCollection);
        return table.GetEntry(key) != null;
    }
    
    public void SetOtherString(string collection, string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable(collection);
        if (table == null)
        {
            Debug.LogError($"本地化Collection [{collection}] 不存在, 请先创建");
        }
        else
        {
            StringReference.Arguments = args;
            StringReference.SetReference(collection, key);
        }
    }
}