using System.Collections;
using UnityEditor;
using UnityEditor.Localization.UI;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(PgLocalizeStringEvent))]
class PgLocalizeStringEventEditor : LocalizeStringEventEditor
{
    //public void ForceRefresh()
    //{
    //    EditorCoroutines.Execute(ForceRefreshFunc(((PgLocalizeStringEvent)target).gameObject));
    //}

    //static IEnumerator ForceRefreshFunc(GameObject obj)
    //{
    //    yield return new WaitForEndOfFrame();
    //    Selection.activeObject = obj;
    //}
}
