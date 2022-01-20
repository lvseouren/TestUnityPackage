using MogulTech.Utilities;
using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PgLocalizeComponent_UGUI
{
    [MenuItem("CONTEXT/Text/PgLocalize")]
    static void LocalizeUIText(MenuCommand command)
    {
        var target = command.context as Text;
        SetupForLocalization(target);
    }

    [MenuItem("CONTEXT/TextMeshProUGUI/PgLocalize")]
    static void LocalizeUITmp(MenuCommand command)
    {
        var target = command.context as TextMeshProUGUI;
        SetupForLocalization(target);
    }

    [MenuItem("CONTEXT/PgLocalizeStringEvent/PgLocalize-AutoSetKey")]
    static void LocalizeAutoSetKey(MenuCommand command)
    {
        var target = command.context as PgLocalizeStringEvent;
        var textComp = target.GetComponent<TextMeshProUGUI>();
        target.StringReference.TableReference = LocalizationSettings.StringDatabase.DefaultTable;
        if (textComp)
            SearchKeyAndSet(target, textComp.text);
        EditorCoroutines.Execute(DelaySetActiveObj(target.gameObject));
    }

    static IEnumerator DelaySetActiveObj(GameObject obj)
    {
        yield return new WaitForEndOfFrame();
        Selection.activeObject = obj;
    }

    public static MonoBehaviour SetupForLocalization(Text target)
    {
        var comp = Undo.AddComponent(target.gameObject, typeof(PgLocalizeStringEvent)) as PgLocalizeStringEvent;
        var setStringMethod = target.GetType().GetProperty("text").GetSetMethod();
        var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setStringMethod) as UnityAction<string>;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
        comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

        const int kMatchThreshold = 5;
        var foundKey = LocalizationEditorSettings.FindSimilarKey(target.text);
        if (foundKey.collection != null && foundKey.matchDistance < kMatchThreshold)
        {
            comp.StringReference.TableEntryReference = foundKey.entry.Id;
            comp.StringReference.TableReference = foundKey.collection.TableCollectionNameReference;
        }

        return comp;
    }

    static void SearchKeyAndSet(PgLocalizeStringEvent comp, string text)
    {
        var key = FindKeyInternal(text);
        if (key > 0)
            comp.StringReference.TableEntryReference = key;

        Selection.activeObject = null;
    }

    public static MonoBehaviour SetupForLocalization(TextMeshProUGUI target)
    {
        var comp = Undo.AddComponent(target.gameObject, typeof(PgLocalizeStringEvent)) as PgLocalizeStringEvent;
        comp.StringReference.TableReference = LocalizationSettings.StringDatabase.DefaultTable;
        SearchKeyAndSet(comp, target.text);

        var setStringMethod = target.GetType().GetProperty("text").GetSetMethod();
        var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setStringMethod) as UnityAction<string>;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
        comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

        const int kMatchThreshold = 5;
        var foundKey = LocalizationEditorSettings.FindSimilarKey(target.text);
        if (foundKey.collection != null && foundKey.matchDistance < kMatchThreshold)
        {
            comp.StringReference.TableEntryReference = foundKey.entry.Id;
            comp.StringReference.TableReference = foundKey.collection.TableCollectionNameReference;
        }

        return comp;
    }

    internal static long FindKeyInternal(string text)
    {
        if (string.IsNullOrEmpty(text))
            return -1;

        var mapContentToKey = PgLocalizeTool.GetMapContentToKey();
        if (mapContentToKey.TryGetValue(text, out string collection_id))
        {
            var data = collection_id.Split('_');
            var colloection = LocalizationSettings.StringDatabase.DefaultTable;
            var id = Convert.ToInt64(data[1]);
            return id;
        }
        return -1;
    }
}
