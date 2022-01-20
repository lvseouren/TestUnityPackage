using NetHelper;
using System.Collections;
using System;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Web;
using UnityEngine;
using Random = System.Random;

public class TranslateResult
{
    public int errorCode { get; set; }
    public List<string> translation { get; set; }
    public BasicTranslation basic { get; set; }
    public List<WebTranslation> web { get; set; }
}

public class BaiduTranslateData
{
    public int error_code { get; set; }
    public string error_msg { get; set; }
    public string from { get; set; }
    public string to { get; set; }
    public List<BaiduTranslateResult> trans_result { get; set; }
}

public class BaiduTranslateResult
{
    public string src { get; set; }
    public string dst { get; set; }
}

public class BasicTranslation
{
    public string phonetic { get; set; }
    public List<string> explains { get; set; }
}

public class WebTranslation
{
    public string key { get; set; }
    public List<string> value { get; set; }
}

public static class Translator
{
    private const string youdaoApiUrl = "http://openapi.youdao.com/api";

    //https://ai.youdao.com/doc.s#guide
    private const string appId_Youdao = "69d3effe05614675";
    private const string appKey_Youdao = "TCopq8UTfrfLbXgPZDrh4hTjLikqMBjr";

    //https://fanyi-api.baidu.com/doc/21
    private const string appId_Baidu = "20211229001041630";
    private const string appKey_Baidu = "bfPWQOzRZ7e9CD1unTaM";
    public static string GetTranslate(string query, bool isUseBaidu = true)
    {
        if(isUseBaidu)
            return GetTranslateByBaidu(query);
        else
            return GetTranslateByYoudao(query);
    }

    static string GetTranslateByBaidu(string query)
    {
        string q = query;
        // 源语言
        string from = "auto";
        // 目标语言
        string to = "auto";
        // 改成您的APP ID
        string appId = appId_Baidu;
        Random rd = new Random();
        string salt = rd.Next(100000).ToString();
        // 改成您的密钥
        string secretKey = appKey_Baidu;
        string sign = EncryptString(appId + q + salt + secretKey);
        string url = "http://api.fanyi.baidu.com/api/trans/vip/translate?";
        url += "q=" + HttpUtility.UrlEncode(q);
        url += "&from=" + from;
        url += "&to=" + to;
        url += "&appid=" + appId;
        url += "&salt=" + salt;
        url += "&sign=" + sign;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.ContentType = "text/html;charset=UTF-8";
        request.UserAgent = null;
        request.Timeout = 6000;
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream myResponseStream = response.GetResponseStream();
        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
        string retString = myStreamReader.ReadToEnd();
        myStreamReader.Close();
        myResponseStream.Close();
        Debug.Log(retString);
        var ret = JsonConvert.DeserializeObject<BaiduTranslateData>(retString);
        if (ret.error_code > 0)
            return ret.error_msg;
        else
            return ret.trans_result[0].dst;
    }

    static string GetTranslateByYoudao(string query)
    {
        var url = GetRequestUrl(query);
        var json = Http.HttpGetRequest(url);
        TranslateResult o = JsonConvert.DeserializeObject<TranslateResult>(json);
        if (o.errorCode > 0)
            return null;
        return o.translation[0];
    }

    static string GetRequestUrl(string query)
    {
        string from = "auto";
        string to = "auto";
        string salt = "6";
        string sign = CalculateMD5Hash(appId_Youdao + query + salt + appKey_Youdao);
        string url = $"{youdaoApiUrl}?q={query}&from={from}&to={to}&appKey={appId_Youdao}&salt={salt}&sign={sign}";
        return url;
        //Dictionary<string, string> dic = new Dictionary<string, string>();
        //string appKey = "69d3effe05614675";
        //string appSecret = "TCopq8UTfrfLbXgPZDrh4hTjLikqMBjr";
        //string url = "https://openapi.youdao.com/file_trans/query=";
        //string flownumber = "文件流水号";
        //string salt = DateTime.Now.Millisecond.ToString();
        //TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        //long millis = (long)ts.TotalMilliseconds;
        //string curtime = Convert.ToString(millis / 1000);
        //string signStr = appKey + Truncate(flownumber) + salt + curtime + appSecret; ;
        //string sign = ComputeHash(signStr, new SHA256CryptoServiceProvider());
        //dic.Add("flownumber", flownumber);
        //dic.Add("appKey", appKey);
        //dic.Add("salt", salt);
        //dic.Add("curtime", curtime);
        //dic.Add("sign", sign);
        //dic.Add("docType", "json");
        //dic.Add("signType", "v3");

        //StringBuilder builder = new StringBuilder();
        //int i = 0;
        //foreach (var item in dic)
        //{
        //    if (i > 0)
        //        builder.Append("&");
        //    builder.AppendFormat("{0}={1}", item.Key, item.Value);
        //    i++;
        //}
        //return url + query + "&" + builder.ToString();
    }

    static string CalculateMD5Hash(string input)
    {
        MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        byte[] hash = md5.ComputeHash(inputBytes);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }

    static string ComputeHash(string input, HashAlgorithm algorithm)
    {
        Byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);
        return BitConverter.ToString(hashedBytes).Replace("-", "");
    }

    static string Truncate(string q)
    {
        if (q == null)
        {
            return null;
        }
        int len = q.Length;
        return len <= 20 ? q : (q.Substring(0, 10) + len + q.Substring(len - 10, 10));
    }

    static string Post(string url, Dictionary<string, string> dic)
    {
        string result = "";
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "POST";
        req.ContentType = "application/x-www-form-urlencoded";
        StringBuilder builder = new StringBuilder();
        int i = 0;
        foreach (var item in dic)
        {
            if (i > 0)
                builder.Append("&");
            builder.AppendFormat("{0}={1}", item.Key, item.Value);
            i++;
        }
        byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
        req.ContentLength = data.Length;
        using (Stream reqStream = req.GetRequestStream())
        {
            reqStream.Write(data, 0, data.Length);
            reqStream.Close();
        }
        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
        Stream stream = resp.GetResponseStream();
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            result = reader.ReadToEnd();
        }
        return result;
    }

    static string EncryptString(string str)
    {
        MD5 md5 = MD5.Create();
        // 将字符串转换成字节数组
        byte[] byteOld = Encoding.UTF8.GetBytes(str);
        // 调用加密方法
        byte[] byteNew = md5.ComputeHash(byteOld);
        // 将加密结果转换为字符串
        StringBuilder sb = new StringBuilder();
        foreach (byte b in byteNew)
        {
            // 将字节转换成16进制表示的字符串，
            sb.Append(b.ToString("x2"));
        }
        // 返回加密的字符串
        return sb.ToString();
    }
}

