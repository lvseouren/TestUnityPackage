
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class PgLocalizeTool
{
    const string kUIEffectRootPath = "Assets/Resources/Effects";
    const string AutoGenCollectionName = "OneForAll";
    const string collectionRoot = "Assets/Localization/Collections/";
    static string[] prefabRoots = { "Assets/Resources/UI", "Assets/AssetBundles/UI/Modules" };

    static Dictionary<string, string> mapContentToKey = new Dictionary<string, string>();
    //static Dictionary<long, string> mapKeyToCollection = new Dictionary<long, string>();
    static Dictionary<string, bool> dictCollection = new Dictionary<string, bool>();

    public static Dictionary<string, string> GetMapContentToKey()
    {
        BuildDataDictionary();
        return mapContentToKey;
    }

    public static void BuildDataDictionary()
    {
        if (mapContentToKey.Count > 0)
            return;
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();
        foreach (var collection in stringTableCollections)
        {
            dictCollection.Add(collection.name, true);
            var cn_table = collection.GetTable("zh-CN");
            foreach (var entry in cn_table.TableData)
            {
                if (!mapContentToKey.ContainsKey(entry.Localized))
                {
                    //mapKeyToCollection.Add(entry.Id, collection.name);
                    mapContentToKey.Add(entry.Localized, collection.name + "_" + entry.Id.ToString());
                }
            }
        }
    }

    [MenuItem("Localization/Tool/prefab添加本地化组件")]
    static void ProcessPrefabs()
    {
        mapContentToKey.Clear();
        dictCollection.Clear();
        //build data dictionary
        BuildDataDictionary();

        //traverse all prefab
        foreach(var prefabRoot in prefabRoots)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            Process(guids);
        }
        //ProcessRemove(guids);
        //get prefab all tmp component
        //check has pglocalize component and set key
    }

    static void Process(string[] guids)
    {
        int length = guids.Length;

        //遍历list，对每一个prefab检查是否引用了被查prefab(how--check whether contain guid or not)
        for (int i = 0; i < length; i++)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (filePath.StartsWith(kUIEffectRootPath))
                continue;
            Debug.Log(filePath);
            var prefab = PrefabUtility.LoadPrefabContents(filePath);
            bool markDirty = ProcessPrefab(prefab, true);
            if (markDirty)
                PrefabUtility.SaveAsPrefabAsset(prefab, filePath);
            PrefabUtility.UnloadPrefabContents(prefab);

            EditorUtility.DisplayProgressBar("Processing...", string.Format("Processed {0}/{1}", i, length), i / (float)length);
        }
        EditorUtility.ClearProgressBar();
    }

    public static bool ProcessPrefab(GameObject prefab, bool isForceAddComp=false)
    {
        var txts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
        bool markDirty = false;
        foreach (var txt in txts)
        {
            TrySetPgLocalizeComponent(txt, isForceAddComp);
            markDirty = true;
        }
        return markDirty;
    }

    static void ProcessRemove(string[] guids)
    {
        int length = guids.Length;

        //遍历list，对每一个prefab检查是否引用了被查prefab(how--check whether contain guid or not)
        for (int i = 0; i < length; i++)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (filePath.StartsWith(kUIEffectRootPath))
                continue;
            Debug.Log(filePath);
            var prefab = PrefabUtility.LoadPrefabContents(filePath);
            var comps = prefab.GetComponentsInChildren<PgLocalizeStringEvent>();
            bool markDirty = false;
            foreach(var pgCom in comps)
            {
                if (!pgCom.StringReference.IsEmpty && pgCom.StringReference.TableReference == AutoGenCollectionName)
                {
                    GameObject.DestroyImmediate(pgCom);
                    markDirty = true;
                }
            }
            if (markDirty)
                PrefabUtility.SaveAsPrefabAsset(prefab, filePath);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    static void TrySetPgLocalizeComponent(TextMeshProUGUI text, bool isForceAddComp = false)
    {
        if (!CheckIsChineseString(text.text))
            return;
        var pgComp = text.GetComponent<PgLocalizeStringEvent>();
        if (pgComp == null)
        {
            if (isForceAddComp)
                pgComp = text.gameObject.AddComponent<PgLocalizeStringEvent>();
            else
                return;
        }
        var setStringMethod = text.GetType().GetProperty("text").GetSetMethod();
        var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), text, setStringMethod) as UnityAction<string>;

        if (pgComp.OnUpdateString.GetPersistentEventCount() == 0)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pgComp.OnUpdateString, methodDelegate);
        pgComp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

        if (pgComp.StringReference.IsEmpty || !dictCollection.ContainsKey(pgComp.StringReference.TableReference) || LocalizationEditorSettings.GetStringTableCollection(pgComp.StringReference.TableReference).SharedData.GetEntry(pgComp.StringReference.TableEntryReference.KeyId) == null)
        {
            Debug.LogWarning(pgComp.gameObject.name);
            SetPgLocalizeKeyByContent(pgComp, text.text, isForceAddComp);
        }
    }

    static void SetPgLocalizeKeyByContent(PgLocalizeStringEvent comp, string value, bool isForceSet = false)
    {
        var reference = comp.StringReference;
        //get collection name and entry key by content
        if (mapContentToKey.TryGetValue(value, out string collection_id))
        {
            var data = collection_id.Split('_');
            var colloection = LocalizationSettings.StringDatabase.DefaultTable;
            var id = Convert.ToInt64(data[1]);
            reference.TableReference = colloection;
            reference.TableEntryReference = id;
        }
        else
        {
            if (!isForceSet)
                return;
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationSettings.StringDatabase.DefaultTable);
            if (collection == null)
            {
                string dir = collectionRoot;
                collection = LocalizationEditorSettings.CreateStringTableCollection(LocalizationSettings.StringDatabase.DefaultTable, dir);
            }
            var cnt = collection.SharedData.Entries.Count;
            var key = $"{AutoGenCollectionName}_Key{cnt + 1}";
            var shareEntry = collection.SharedData.AddKey(key);
            var locales = LocalizationEditorSettings.GetLocales();
            for (int i = 0; i < locales.Count; ++i)
            {
                var localTable = (collection.GetTable(locales[i].Identifier) as StringTable);
                var entry = localTable.GetEntry(key);
                string localizedString = locales[i].Identifier.Code == "en" ? key : value;

                var sharedEntry = collection.SharedData.GetEntry(key);
                if (sharedEntry == null)
                {
                    sharedEntry = collection.SharedData.AddKey(key);
                    EditorUtility.SetDirty(collection.SharedData);
                    localTable.AddEntry(sharedEntry.Key, localizedString);
                    EditorUtility.SetDirty(localTable);
                }
                else if (entry == null)
                {
                    localTable.AddEntry(sharedEntry.Key, localizedString);
                    EditorUtility.SetDirty(localTable);
                }
                else
                    entry.Value = localizedString;
            }

            reference.TableReference = LocalizationSettings.StringDatabase.DefaultTable;
            reference.TableEntryReference = key;
            mapContentToKey.Add(value, LocalizationSettings.StringDatabase.DefaultTable + "_" + shareEntry.Id);
            AssetDatabase.Refresh();
        }
        comp.StringReference = reference;
    }

    static bool CheckIsChineseString(string s)
    {
        char[] c = s.ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] >= 0x4e00 && c[i] <= 0x9fbb)
                return true;
            else
                return false;
        }
        return false;
    }
}

