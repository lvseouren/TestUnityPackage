using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor.Localization;
using UnityEditor;
using UnityEngine.Localization.Metadata;
using System.Dynamic;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
//using xlsxToCsv;
using OfficeOpenXml;
using UnityEngine.Localization.Settings;

public static class PgExcelExtension
{
    public const bool isUseOneCollection = true;//是否只是用一个Collection（即不分模块）

    const string xlsxFileName = "Assets/Localization/XLSX/Localization.xlsx";
    const string collectionRoot = "Assets/Localization/Collections/";

    const string testFile = "Assets/Localization/CSV/testExportXlsx.xlsx";
    //[MenuItem("Localization/Excel/Test")]
    public static void TestExport()
    {
        Excel xlsx = ExcelHelper.LoadExcel(testFile);
        if (xlsx == null)
            xlsx = ExcelHelper.CreateExcel(testFile);
        if (xlsx.Tables.Count == 0)
            xlsx.AddTable("sheet");
        var sheet = xlsx.Tables[0];
        sheet.SetValue(0, 1, "第0行第1列");
        sheet.SetValue(1, 1, "第1行第1列");
        sheet.SetValue(2, 2, "第2行第2列");
        sheet.SetValue(3, 4, "第3行第4列");
        ExcelHelper.SaveExcel(xlsx, testFile);
    }

    //将所有Module Collection(string类型)的数据导出到一个xlsx文件中
    [MenuItem("Localization/Excel/导出数据到xlsx")]
    public static void ExportAllCollection()
    {
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        Excel xlsx = ExcelHelper.LoadExcel(xlsxFileName);
        if(xlsx == null || true)
            xlsx = ExcelHelper.CreateExcel(xlsxFileName);
        if (xlsx.Tables.Count == 0)
            xlsx.AddTable("sheet");
        var sheet = xlsx.Tables[0];
       

        //csvWriter.WriteField("Key");
        sheet.SetValue(1, 1, "Key");

        var locales = LocalizationEditorSettings.GetLocales();
        for (int i = 0; i < locales.Count; ++i)
        {
            sheet.SetValue(1, i+2, locales[i].name);
        }

        var rowIndex = 2;
        foreach (var collection in stringTableCollections)
        {
            //写入collection 名称，写入分割content--不需要
            //写入collection内容
            //csvWriter.WriteField(collection.name);
            //csvWriter.NextRecord();
            foreach (var row in collection.GetRowEnumerator())
            {
                if (row.TableEntries[0] != null && row.TableEntries[0].SharedEntry.Metadata.HasMetadata<ExcludeEntryFromExport>())
                    continue;

                //csvWriter.WriteField(row.KeyEntry.Key);
                sheet.SetValue(rowIndex, 1, row.KeyEntry.Key);

                //int i = 0;
                //foreach(var table in collection.StringTables)
                //{
                //    var entry = row.TableEntries[i++];
                //    if(entry!=null)
                //        csvWriter.WriteField(entry.LocalizedValue, true);
                //}
                for (int i = 0; i < locales.Count; ++i)
                {
                    var locale = locales[i];
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    var entry = table.GetEntry(row.KeyEntry.Key);
                    if (entry != null)
                        //csvWriter.WriteField(entry.LocalizedValue);
                        sheet.SetValue(rowIndex, i + 2, entry.LocalizedValue);
                    else
                        sheet.SetValue(rowIndex, i + 2, "");
                }
                rowIndex++;
            }
        }
        ExcelHelper.SaveExcel(xlsx, xlsxFileName);
    }

    [MenuItem("Localization/Excel/xlsx数据的导入")]
    //读取总表，并刷新对应Collection中的数据
    public static void ImportAll()
    {
        Excel xlsx = ExcelHelper.LoadExcel(xlsxFileName);
        if (xlsx == null)
            return;
        if (xlsx.Tables.Count == 0)
            return;
        var sheet = xlsx.Tables[0];
        var colCnt = sheet.NumberOfColumns;
        Dictionary<string, int> mapLocaleToCol = new Dictionary<string, int>();
        for(int i = 2; i<=colCnt;++i)
        {
            mapLocaleToCol.Add(sheet.GetCell(1, i).Value, i);
        }
        for(int i=2;i<=sheet.NumberOfRows;++i)
        {
            var key = sheet.GetCell(i, 1).Value;
            var collectionName = GetCollectionNameByKey(key);
            var locales = LocalizationEditorSettings.GetLocales();
            foreach (Locale locale in locales)
            {
                if (!mapLocaleToCol.ContainsKey(locale.name))
                    continue;
                var col = mapLocaleToCol[locale.name];
                var value = sheet.GetCell(i, col).Value;

                LocalizationTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(collectionName);
                if (collection == null)
                {
                    string dir = collectionRoot;
                    collection = LocalizationEditorSettings.CreateStringTableCollection(collectionName, dir);
                }
                StringTable localTable = collection.GetTable(locale.Identifier) as StringTable;
                if (localTable == null)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        localTable = collection.AddNewTable(locale.Identifier) as StringTable;
                    }
                    else
                        continue;
                }
                StringTableEntry entry = localTable.GetEntry(key);
                var sharedEntry = collection.SharedData.GetEntry(key);
                if (sharedEntry == null)
                {
                    sharedEntry = collection.SharedData.AddKey(key);
                    EditorUtility.SetDirty(collection.SharedData);
                    localTable.AddEntry(sharedEntry.Key, value);
                    //LocalizationEditorSettings.EditorEvents.TableEntryModified.Invoke(sharedEntry);
                }
                else if (entry == null)
                {
                    localTable.AddEntry(sharedEntry.Key, value);
                }
                else
                    entry.Value = value;
                
                EditorUtility.SetDirty(localTable);
            }
        }
    }

    [MenuItem("Localization/Excel/Careful/全量Import The OneForAll XLSX")]
    // 全量读取总表，并刷新对应Collection中的数据
    public static void ImportAllFull()
    {
        Dictionary<LocalizationTableCollection, bool> relatedCollectionDict = new Dictionary<LocalizationTableCollection, bool>();
        Dictionary<string, Dictionary<string, bool>> collectionImportedKeyDict = new Dictionary<string, Dictionary<string, bool>>();
        Excel xlsx = ExcelHelper.LoadExcel(xlsxFileName);
        if (xlsx == null)
            return;
        if (xlsx.Tables.Count == 0)
            return;
        var sheet = xlsx.Tables[0];
        var colCnt = sheet.NumberOfColumns;
        Dictionary<string, int> mapLocaleToCol = new Dictionary<string, int>();
        for (int i = 2; i <= colCnt; ++i)
        {
            mapLocaleToCol.Add(sheet.GetCell(1, i).Value, i);
        }
        for (int i = 2; i <= sheet.NumberOfRows; ++i)
        {

            var key = sheet.GetCell(i, 1).Value;
            var collectionName = GetCollectionNameByKey(key);
            var locales = LocalizationEditorSettings.GetLocales();
            foreach (Locale locale in locales)
            {
                if (!mapLocaleToCol.ContainsKey(locale.name))
                    continue;
                var col = mapLocaleToCol[locale.name];
                var value = sheet.GetCell(i, col).Value;

                LocalizationTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(collectionName);
                if (collection == null)
                {
                    string dir = collectionRoot;
                    collection = LocalizationEditorSettings.CreateStringTableCollection(collectionName, dir);
                }
                if (!relatedCollectionDict.ContainsKey(collection))
                    relatedCollectionDict.Add(collection, true);

                StringTable localTable = collection.GetTable(locale.Identifier) as StringTable;
                if (localTable == null)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        localTable = collection.AddNewTable(locale.Identifier) as StringTable;
                    }
                    else
                        continue;
                }
                StringTableEntry entry = localTable.GetEntry(key);
                if (entry == null)
                    localTable.AddEntry(key, value);
                else
                    entry.Value = value;

                if (!collectionImportedKeyDict.ContainsKey(collection.TableCollectionName))
                    collectionImportedKeyDict.Add(collection.TableCollectionName, new Dictionary<string, bool>());
                var keyDict = collectionImportedKeyDict[collection.TableCollectionName];
                if (!keyDict.ContainsKey(key))
                    keyDict.Add(key, true);
            }
        }

        //对已经导入的collection，遍历key，如果key没被导入，则删掉
        List<string> tobeDelete = new List<string>();
        foreach (var data in relatedCollectionDict)
        {
            tobeDelete.Clear();
            var collection = data.Key;
            var importedKeyDict = collectionImportedKeyDict[collection.TableCollectionName];
                        
            foreach (var entry in collection.SharedData.Entries)
            {
                var key = entry.Key;
                if (!importedKeyDict.ContainsKey(key))
                    //collection.RemoveEntry(key);
                    tobeDelete.Add(key);
            }
            for(int i = 0;i<tobeDelete.Count;++i)
            {
                collection.RemoveEntry(tobeDelete[i]);
            }
        }
    }

    static string GetCollectionNameByKey(string key)
    {
        if (isUseOneCollection)
            return LocalizationSettings.StringDatabase.DefaultTable;
        else
        {
            var data = key.Split('_');
            return data[0];
        }
    }

    #region tool

    [MenuItem("Localization/Tool/机翻/百度翻译")]
    static void BaiduTranslate()
    {
        Translate(true);
    }

    [MenuItem("Localization/Tool/机翻/有道翻译")]
    static void YoudaoTranslate()
    {
        Translate(false);
    }

    static void Translate(bool isUseBaidu = true)
    {
        Excel xlsx = ExcelHelper.LoadExcel(xlsxFileName);
        if (xlsx == null)
            return;
        if (xlsx.Tables.Count == 0)
            return;
        var sheet = xlsx.Tables[0];
        var rowCnt = sheet.NumberOfRows;
        for (int i = 2; i <= rowCnt; ++i)
        {
            var value = sheet.GetCell(i, 2).Value;
            value = Translator.GetTranslate(value, isUseBaidu);
            if(!string.IsNullOrEmpty(value))
            {
                var oldValue = sheet.GetCell(i, 3).Value;
                if(string.IsNullOrEmpty(oldValue))
                    sheet.SetValue(i, 3, value);
            }
        }
        ExcelHelper.SaveExcel(xlsx, xlsxFileName);
    }

    [MenuItem("Localization/Tool/首字母改为大写")]
    public static void PreProcessExcel()
    {
        var file = "Assets/Localization/CSV/OneForAll2.xlsx";
        Excel xlsx = ExcelHelper.LoadExcel(file);
        if (xlsx == null)
            return;
        if (xlsx.Tables.Count == 0)
            return;
        var sheet = xlsx.Tables[0];
        var rowCnt = sheet.NumberOfRows;
        for (int i = 1; i < rowCnt; ++i)
        {
            var value = sheet.GetCell(i, 1).Value;
            value = value.Substring(0, 1).ToUpper() + value.Substring(1);
            sheet.SetValue(i, 1, value);
        }
        ExcelHelper.SaveExcel(xlsx, file);
    }
    #endregion
}

