/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections.Specialized;

using Azos.Graphics;
using Azos.Conf;
using Azos.Serialization.JSON;


namespace Azos.Web
{
  public enum HTTPRequestMethod { GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE, PATCH };

  /// <summary>
  /// Stipulates contract for an entity that executes calls via WebClient
  /// </summary>
  public interface IWebClientCaller
  {
    /// <summary>
    /// Specifies timeout for web call. If zero then default is used
    /// </summary>
    int WebServiceCallTimeoutMs { get; }

    /// <summary>
    /// Specifies keep-alive option for web request
    /// </summary>
    bool KeepAlive { get; }

    /// <summary>
    /// Specifies if pipelining is used for web request
    /// </summary>
    bool Pipelined { get; }
  }


#warning WebClient::This needs review per http://www.diogonunes.com/blog/webclient-vs-httpclient-vs-httpwebrequest/

  /// <summary>
  /// Facilitates working with external services provided via HTTP
  /// </summary>
  public static partial class WebClient
  {
    #region Inner
    /// <summary>
    /// Provides request parameters for making WebClient calls
    /// </summary>
    public struct RequestParams
    {
      public RequestParams(IWebClientCaller caller)
      {
        Caller = caller;

        Method = HTTPRequestMethod.GET;
        ContentType = Web.ContentType.FORM_URL_ENCODED;
        QueryParameters = null;
        BodyParameters = null;
        Body = null;
        Headers = null;
        AcceptType = null;
        UserName = null;
        Password = null;
      }

      #region Properties
      public readonly IWebClientCaller Caller;

      public HTTPRequestMethod Method;
      public IDictionary<string, string> QueryParameters;
      public IDictionary<string, string> BodyParameters;
      public string Body;
      public IDictionary<string, string> Headers;
      public string ContentType;
      public string AcceptType;
      public string UserName, Password;

      public bool HasCredentials { get { return UserName.IsNotNullOrWhiteSpace(); } }
      public bool HasBody { get { return Body != null || (BodyParameters != null && BodyParameters.Count > 0); } }
      #endregion

      #region Public
      public ICredentials GetCredentials() { return new NetworkCredential(UserName, Password); }

      public WebHeaderCollection GetHeaders()
      {
        var result = new WebHeaderCollection();
        if (Headers != null)
          Headers.ForEach(kvp => result.Add(kvp.Key, kvp.Value));

        if (ContentType.IsNotNullOrWhiteSpace())
          result.Add(HttpRequestHeader.ContentType, ContentType);
        if (AcceptType.IsNotNullOrWhiteSpace())
          result.Add(HttpRequestHeader.Accept, AcceptType);

        return result;
      }

      public NameValueCollection GetQueryString()
      {
        if (QueryParameters == null)
          return new NameValueCollection();
        var queryParams = new NameValueCollection(QueryParameters.Count);
        foreach (var param in QueryParameters)
          queryParams.Add(param.Key, param.Value);
        return queryParams;
      }

      public string GetBody()
      {
        return Body ?? ((BodyParameters != null && BodyParameters.Count > 0) ? string.Join("&", BodyParameters.Select(p => p.Key + "=" + p.Value)) : string.Empty);
      }
      #endregion
    }

    #endregion

    #region Public
    public static string GetString(string uri, RequestParams request) { return GetString(new Uri(uri), request); }

    public static string GetString(Uri uri, RequestParams request)
    {
      return Uri.UnescapeDataString(DoRequest((client) =>
      {
        if (request.Method == HTTPRequestMethod.GET && !request.HasBody)
          return client.DownloadString(uri);
        if (request.Method == HTTPRequestMethod.POST)
          return client.UploadString(uri, request.GetBody());
        return client.UploadString(uri, request.Method.ToString(), request.GetBody());
      }, request));
    }

    public static Task<string> GetStringAsync(string uri, RequestParams request) { return GetStringAsync(new Uri(uri), request); }

    public static Task<string> GetStringAsync(Uri uri, RequestParams request)
    {
      return DoRequest((client) =>
      {
        if (request.Method == HTTPRequestMethod.GET && !request.HasBody)
          return client.DownloadStringTaskAsync(uri);
        if (request.Method == HTTPRequestMethod.POST)
          return client.UploadStringTaskAsync(uri, request.GetBody());
        return client.UploadStringTaskAsync(uri, request.Method.ToString(), request.GetBody());
      }, request).ContinueWith((antecedent) => Uri.UnescapeDataString(antecedent.Result));
    }

    public static byte[] GetData(string uri, RequestParams request) { string contentType; return GetData(new Uri(uri), request, out contentType); }

    public static byte[] GetData(string uri, RequestParams request, out string contentType) { return GetData(new Uri(uri), request, out contentType); }

    public static byte[] GetData(Uri uri, RequestParams request) { string contentType; return GetData(uri, request, out contentType); }

    public static byte[] GetData(Uri uri, RequestParams request, out string contentType)
    {
      string localContentType = string.Empty;
      var data = DoRequest((client) =>
      {
        var result = client.DownloadData(uri);
        localContentType = client.ResponseHeaders[HttpResponseHeader.ContentType];
        return result;
      }, request);
      contentType = localContentType;
      return data;
    }

    public static Task<byte[]> GetDataAsync(string uri, RequestParams request) { return GetDataAsync(new Uri(uri), request); }

    public static Task<byte[]> GetDataAsync(Uri uri, RequestParams request)
    {
      return DoRequest((client) => client.DownloadDataTaskAsync(uri), request);
    }

    public static Task<Tuple<byte[], string>> GetDataAsyncWithContentType(string uri, RequestParams request) { return GetDataAsyncWithContentType(new Uri(uri), request); }

    public static Task<Tuple<byte[], string>> GetDataAsyncWithContentType(Uri uri, RequestParams request)
    {
      return DoRequest((client) =>
        client.DownloadDataTaskAsync(uri)
          .ContinueWith((antecedent) => Tuple.Create(antecedent.Result, client.ResponseHeaders[HttpResponseHeader.ContentType])), request);
    }

    public static Image GetImage(string uri, RequestParams request) { return GetImage(new Uri(uri), request); }

    public static Image GetImage(Uri uri, RequestParams request)
    {
      var data = GetData(uri, request);

      using (var stream = new MemoryStream(data))
        return Image.FromStream(stream);
    }

    public static Task<Image> GetImageAsync(string uri, RequestParams request) { return GetImageAsync(new Uri(uri), request); }

    public static Task<Image> GetImageAsync(Uri uri, RequestParams request)
    {
      return GetDataAsync(uri, request)
        .ContinueWith((antecedent) =>
        {
          using (var stream = new MemoryStream(antecedent.Result))
            return Image.FromStream(stream);
        });
    }

    public static void GetFile(string uri, RequestParams request, string file) { GetFile(new Uri(uri), request, file); }

    public static void GetFile(Uri uri, RequestParams request, string file)
    {
      DoRequest((client) => { client.DownloadFile(uri, file); }, request);
    }

    public static Task GetFileAsync(string uri, RequestParams request, string file) { return GetFileAsync(new Uri(uri), request, file); }

    public static Task GetFileAsync(Uri uri, RequestParams request, string file)
    {
      return DoRequest((client) => client.DownloadFileTaskAsync(uri, file), request);
    }

    public static JsonDynamicObject GetJsonAsDynamic(string uri, RequestParams request) { return GetJsonAsDynamic(new Uri(uri), request); }

    public static JsonDynamicObject GetJsonAsDynamic(Uri uri, RequestParams request)
    {
      string response = GetString(uri, request);
      return response.IsNotNullOrWhiteSpace() ? response.JsonToDynamic() : null;
    }

    public static Task<JsonDynamicObject> GetJsonAsDynamicAsync(string uri, RequestParams request) { return GetJsonAsDynamicAsync(new Uri(uri), request); }

    public static Task<JsonDynamicObject> GetJsonAsDynamicAsync(Uri uri, RequestParams request)
    {
      return GetStringAsync(uri, request)
        .ContinueWith((antecedent) => {
          var response = antecedent.Result;
          return response.IsNotNullOrWhiteSpace() ? response.JsonToDynamic() as JsonDynamicObject : null;
        });
    }

    public static JsonDataMap GetJson(string uri, RequestParams request) { return GetJson(new Uri(uri), request); }

    public static JsonDataMap GetJson(Uri uri, RequestParams request)
    {
      string response = GetString(uri, request);
      return response.IsNotNullOrWhiteSpace() ? response.JsonToDataObject() as JsonDataMap : null;
    }

    public static Task<JsonDataMap> GetJsonAsync(string uri, RequestParams request) { return GetJsonAsync(new Uri(uri), request); }

    public static Task<JsonDataMap> GetJsonAsync(Uri uri, RequestParams request)
    {
      return GetStringAsync(uri, request)
        .ContinueWith((antecedent) =>
        {
          var response = antecedent.Result;
          return response.IsNotNullOrWhiteSpace() ? response.JsonToDataObject() as JsonDataMap : null;
        });
    }

    public static JsonDataArray GetJsonArray(string uri, RequestParams request) { return GetJsonArray(new Uri(uri), request); }

    public static JsonDataArray GetJsonArray(Uri uri, RequestParams request)
    {
      string response = GetString(uri, request);
      return response.IsNotNullOrWhiteSpace() ? response.JsonToDataObject() as JsonDataArray : null;
    }

    public static Task<JsonDataArray> GetJsonArrayAsync(string uri, RequestParams request) { return GetJsonArrayAsync(new Uri(uri), request); }

    public static Task<JsonDataArray> GetJsonArrayAsync(Uri uri, RequestParams request)
    {
      return GetStringAsync(uri, request)
        .ContinueWith((antecedent) =>
        {
          var response = antecedent.Result;
          return response.IsNotNullOrWhiteSpace() ? response.JsonToDataObject() as JsonDataArray : null;
        });
    }

    public static JsonDataMap GetValueMap(string uri, RequestParams request) { return GetValueMap(new Uri(uri), request); }

    public static JsonDataMap GetValueMap(Uri uri, RequestParams request)
    {
      string response = GetString(uri, request);
      return response.IsNotNullOrWhiteSpace() ? JsonDataMap.FromURLEncodedString(response) : null;
    }

    public static Task<JsonDataMap> GetValueMapAsync(string uri, RequestParams request) { return GetValueMapAsync(new Uri(uri), request); }

    public static Task<JsonDataMap> GetValueMapAsync(Uri uri, RequestParams request)
    {
      return GetStringAsync(uri, request)
        .ContinueWith((antecedent) =>
        {
          var response = antecedent.Result;
          return response.IsNotNullOrWhiteSpace() ? JsonDataMap.FromURLEncodedString(response) : null;
        });
    }

    public static XDocument GetXML(string uri, RequestParams request) { return GetXML(new Uri(uri), request); }

    public static XDocument GetXML(Uri uri, RequestParams request)
    {
      string response = GetString(uri, request);
      return response.IsNotNullOrWhiteSpace() ? XDocument.Parse(response) : null;
    }

    public static Task<XDocument> GetXMLAsync(string uri, RequestParams request) { return GetXMLAsync(new Uri(uri), request); }

    public static Task<XDocument> GetXMLAsync(Uri uri, RequestParams request)
    {
      return GetStringAsync(uri, request)
        .ContinueWith((antecedent) =>
        {
          var response = antecedent.Result;
          return response.IsNotNullOrWhiteSpace() ? XDocument.Parse(response) : null;
        });
    }

    public static T DoRequest<T>(Func<System.Net.WebClient, T> actor, RequestParams request)
    {
      using (var client = new WebClientTimeouted(request.Caller))
      {
        if (request.HasCredentials)
          client.Credentials = request.GetCredentials();

        client.Headers = request.GetHeaders();
        client.QueryString = request.GetQueryString();
        return actor(client);
      }
    }

    public static void DoRequest(Action<System.Net.WebClient> actor, RequestParams request)
    {
      using (var client = new WebClientTimeouted(request.Caller))
      {
        if (request.HasCredentials)
          client.Credentials = request.GetCredentials();

        client.Headers = request.GetHeaders();
        client.QueryString = request.GetQueryString();
        actor(client);
      }
    }

    public static Task<T> DoRequest<T>(Func<System.Net.WebClient, Task<T>> actor, RequestParams request)
    {
      var client = new WebClientTimeouted(request.Caller);
      if (request.HasCredentials)
        client.Credentials = request.GetCredentials();

      client.Headers = request.GetHeaders();
      client.QueryString = request.GetQueryString();
      return actor(client).ContinueWith((antecedent) =>
      {
        client.Dispose();
        return antecedent.Result;
      });
    }

    public static Task DoRequest(Func<System.Net.WebClient, Task> actor, RequestParams request)
    {
      var client = new WebClientTimeouted(request.Caller);
      if (request.HasCredentials)
        client.Credentials = request.GetCredentials();

      client.Headers = request.GetHeaders();
      client.QueryString = request.GetQueryString();
      return actor(client).ContinueWith((antecedent) =>
      {
        client.Dispose();
        return antecedent;
      });
    }
    #endregion
  } //WebClient
}
