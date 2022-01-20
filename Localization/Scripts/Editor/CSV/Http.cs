using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetHelper
{
	public static class Helper
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public static T NonNull<T>(this T obj)
		{
			if (obj == null)
			{
				throw new NullReferenceException();
			}
			return obj;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002061 File Offset: 0x00000261
		public static void RequireNonNull<T>(this T obj)
		{
			if (obj == null)
			{
				throw new NullReferenceException();
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002074 File Offset: 0x00000274
		public static void ValidateDataDirectory(string bundledDataDirectory, string dataDirectory)
		{
			if (!Directory.Exists(dataDirectory))
			{
				Directory.CreateDirectory(dataDirectory);
			}
			foreach (string bundledDataPath in Directory.GetFiles(bundledDataDirectory))
			{
				string data = Path.GetFileName(bundledDataPath);
				string dataPath = Path.Combine(dataDirectory, data.NonNull<string>());
				if (!File.Exists(dataPath))
				{
					File.Copy(bundledDataPath, dataPath);
				}
				else
				{
					DateTime lastWriteTimeUtc = new FileInfo(bundledDataPath).LastWriteTimeUtc;
					DateTime time2 = new FileInfo(dataPath).LastWriteTimeUtc;
					if (lastWriteTimeUtc != time2)
					{
						File.Copy(bundledDataPath, dataPath, true);
					}
				}
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020FC File Offset: 0x000002FC
		public static void ValidateDirectory(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x0000210D File Offset: 0x0000030D
		public static string Formatted<T>(this T t)
		{
			return JsonConvert.SerializeObject(t, Formatting.Indented, new JsonConverter[]
			{
				new StringEnumConverter()
			});
		}
	}

	public class HttpProxy
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600002A RID: 42 RVA: 0x0000296F File Offset: 0x00000B6F
		// (set) Token: 0x0600002B RID: 43 RVA: 0x00002977 File Offset: 0x00000B77
		public bool Enabled { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600002C RID: 44 RVA: 0x00002980 File Offset: 0x00000B80
		// (set) Token: 0x0600002D RID: 45 RVA: 0x00002988 File Offset: 0x00000B88
		public string Server { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002991 File Offset: 0x00000B91
		// (set) Token: 0x0600002F RID: 47 RVA: 0x00002999 File Offset: 0x00000B99
		public int Port { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000030 RID: 48 RVA: 0x000029A2 File Offset: 0x00000BA2
		// (set) Token: 0x06000031 RID: 49 RVA: 0x000029AA File Offset: 0x00000BAA
		public string UserName { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000032 RID: 50 RVA: 0x000029B3 File Offset: 0x00000BB3
		// (set) Token: 0x06000033 RID: 51 RVA: 0x000029BB File Offset: 0x00000BBB
		public string Password { get; set; }
	}

	public static class Http
	{
		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000086 RID: 134 RVA: 0x00002DF9 File Offset: 0x00000FF9
		// (set) Token: 0x06000087 RID: 135 RVA: 0x00002E00 File Offset: 0x00001000
		public static HttpProxy Proxy { private get; set; }

		// Token: 0x06000088 RID: 136 RVA: 0x00002E08 File Offset: 0x00001008
		public static IWebProxy WebProxy()
		{
			if (Http.Proxy == null || !Http.Proxy.Enabled || string.IsNullOrEmpty(Http.Proxy.Server))
			{
				return WebRequest.GetSystemWebProxy();
			}
			if (string.IsNullOrEmpty(Http.Proxy.UserName) || string.IsNullOrEmpty(Http.Proxy.Password))
			{
				return new WebProxy(Http.Proxy.Server, Http.Proxy.Port);
			}
			return new WebProxy(Http.Proxy.Server, Http.Proxy.Port)
			{
				Credentials = new NetworkCredential(Http.Proxy.UserName, Http.Proxy.Password)
			};
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00002EB9 File Offset: 0x000010B9
		public static void Download(string url, string filePath)
		{
			new WebClient
			{
				Proxy = Http.WebProxy(),
				Headers =
				{
					{
						"user-agent",
						"Mozilla/5.0 (Trident/7.0; rv:11.0) like Gecko"
					}
				}
			}.DownloadFile(url, filePath);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00002EE8 File Offset: 0x000010E8
		public static async Task<string> Get(string url, string encoding = "UTF-8")
		{
			HttpWebRequest httpWebRequest = WebRequest.CreateHttp(url);
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 1000;
			httpWebRequest.Proxy = Http.WebProxy();
			httpWebRequest.UserAgent = "Mozilla/5.0 (Trident/7.0; rv:11.0) like Gecko";
			WebResponse webResponse = await httpWebRequest.GetResponseAsync();
			HttpWebResponse response = webResponse as HttpWebResponse;
			response = response.NonNull<HttpWebResponse>();
			Stream stream = response.GetResponseStream().NonNull<Stream>();
			string result;
			using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encoding)))
			{
				string content = await reader.ReadToEndAsync();
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new HttpRequestException(string.Format("Error code <{0}> with content <{1}> returned from <{2}>", response.StatusCode, content, url));
				}
				result = content;
			}
			return result;
		}

		public static string HttpGetRequest(string url)
		{
			string strGetResponse = string.Empty;
			try
			{
				var getRequest = CreateHttpRequest(url, "GET");
				var getResponse = getRequest.GetResponse() as HttpWebResponse;
				strGetResponse = GetHttpResponse(getResponse, "GET");
			}
			catch (Exception ex)
			{
				strGetResponse = ex.Message;
			}
			return strGetResponse;
		}

		private static string GetHttpResponse(HttpWebResponse response, string requestType)
		{
			var responseResult = "";
			const string post = "POST";
			string encoding = "UTF-8";
			if (string.Equals(requestType, post, StringComparison.OrdinalIgnoreCase))
			{
				encoding = response.ContentEncoding;
				if (encoding == null || encoding.Length < 1)
				{
					encoding = "UTF-8";
				}
			}
			using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
			{
				responseResult = reader.ReadToEnd();
			}
			return responseResult;
		}

		private static HttpWebRequest CreateHttpRequest(string url, string requestType, params object[] strJson)
		{
			HttpWebRequest request = null;
			const string get = "GET";
			const string post = "POST";
			if (string.Equals(requestType, get, StringComparison.OrdinalIgnoreCase))
			{
				request = CreateGetHttpWebRequest(url);
			}
			if (string.Equals(requestType, post, StringComparison.OrdinalIgnoreCase))
			{
				request = CreatePostHttpWebRequest(url, strJson[0].ToString());
			}
			return request;
		}

		private static HttpWebRequest CreateGetHttpWebRequest(string url)
		{
			var getRequest = HttpWebRequest.Create(url) as HttpWebRequest;
			getRequest.Method = "GET";
			getRequest.Timeout = 5000;
			getRequest.ContentType = "text/html;charset=UTF-8";
			getRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			return getRequest;
		}

		private static HttpWebRequest CreatePostHttpWebRequest(string url, string postData)
		{
			var postRequest = HttpWebRequest.Create(url) as HttpWebRequest;
			postRequest.KeepAlive = false;
			postRequest.Timeout = 5000;
			postRequest.Method = "POST";
			postRequest.ContentType = "application/x-www-form-urlencoded";
			postRequest.ContentLength = postData.Length;
			postRequest.AllowWriteStreamBuffering = false;
			StreamWriter writer = new StreamWriter(postRequest.GetRequestStream(), Encoding.ASCII);
			writer.Write(postData);
			writer.Flush();
			return postRequest;
		}

		// Token: 0x04000050 RID: 80
		private const string UserAgent = "Mozilla/5.0 (Trident/7.0; rv:11.0) like Gecko";
	}
}