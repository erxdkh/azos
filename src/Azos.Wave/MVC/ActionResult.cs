/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

using Azos;
using Azos.Web;
using Azos.Graphics;
using Azos.Data;
using Azos.Serialization.JSON;

namespace Azos.Wave.Mvc
{
  /// <summary>
  /// Decorates entities that get returned by MVC actions and can get executed to generate some result action (command pattern)
  /// </summary>
  public interface IActionResult
  {
     void Execute(Controller controller, WorkContext work);
  }

  /// <summary>
  /// Represents MVC action result that downloads a local file
  /// </summary>
  public struct FileDownload : IActionResult
  {
    public FileDownload(string fileName, bool isAttachment = false, int bufferSize = Response.DEFAULT_DOWNLOAD_BUFFER_SIZE)
    {
      LocalFileName = fileName;
      IsAttachment = isAttachment;
      BufferSize = bufferSize;
    }


    /// <summary>
    /// Local file name
    /// </summary>
    public readonly string LocalFileName;

    /// <summary>
    /// Download buffer size. Leave unchanged in most cases
    /// </summary>
    public readonly int    BufferSize;

    /// <summary>
    /// When true, asks user to save as attachment
    /// </summary>
    public readonly bool   IsAttachment;



    public void Execute(Controller controller, WorkContext work)
    {
      work.Response.WriteFile(LocalFileName, BufferSize, IsAttachment);
    }

  }

  /// <summary>
  /// Represents MVC action result that downloads binary content
  /// </summary>
  public struct BinaryContent : IActionResult
  {
    public BinaryContent(byte[] data, string contentType = null, string attachmentName = null, int bufferSize = Response.DEFAULT_DOWNLOAD_BUFFER_SIZE)
    {
      Data = data.NonNull(nameof(data));
      ContentType = contentType;
      AttachmentName = attachmentName;
      BufferSize = bufferSize;
    }


    /// <summary>
    /// Binary data to send
    /// </summary>
    public readonly byte[] Data;

    /// <summary>
    /// Content-type header for the binary data
    /// </summary>
    public readonly string ContentType;

    /// <summary>
    /// Download buffer size. Leave unchanged in most cases
    /// </summary>
    public readonly int BufferSize;

    /// <summary>
    /// When true, asks user to save as attachment
    /// </summary>
    public readonly string AttachmentName;



    public void Execute(Controller controller, WorkContext work)
    {
      var ctp = ContentType;
      if (ctp.IsNullOrWhiteSpace())
       ctp = Azos.Web.ContentType.BINARY;

      work.Response.ContentType = ctp;

      using (var ms = new System.IO.MemoryStream(Data))
        work.Response.WriteStream(ms, BufferSize,  AttachmentName);
    }

  }


  /// <summary>
  /// Represents MVC action result that redirects user to some URL
  /// </summary>
  public struct Redirect : IActionResult
  {
    public Redirect(string url, WebConsts.RedirectCode code = WebConsts.RedirectCode.Found_302)
    {
      URL = url;
      Code = code;
    }
    /// <summary>
    /// Where to redirect user
    /// </summary>
    public readonly string URL;

    /// <summary>
    /// Redirect code
    /// </summary>
    public readonly WebConsts.RedirectCode Code;

    public void Execute(Controller controller, WorkContext work)
    {
      work.Response.Redirect(URL, Code);
    }
  }

  /// <summary>
  /// Represents MVC action result that returns/downloads an image
  /// </summary>
  public struct Picture : IActionResult, IDisposable
  {
    public Picture(Image image, ImageFormat format, string attachmentFileName = null)
    {
      m_Image = image;
      m_Format = format;
      m_AttachmentFileName = attachmentFileName;
    }

    private Image       m_Image;
    private ImageFormat m_Format;
    public  string      m_AttachmentFileName;



    /// <summary>
    /// Picture image
    /// </summary>
    public Image Image{ get{ return m_Image;} }

    /// <summary>
    /// Download buffer size. Leave unchanged in most cases
    /// </summary>
    public ImageFormat  Format{ get{ return m_Format;} }

    /// <summary>
    /// When non-null asks user to download picture as a named attached file
    /// </summary>
    public string  AttachmentFileName{ get{ return m_AttachmentFileName;} }



    public void Execute(Controller controller, WorkContext work)
    {
      if (m_Image==null) return;

      if (m_AttachmentFileName.IsNotNullOrWhiteSpace())
          work.Response.Headers.Add(WebConsts.HTTP_HDR_CONTENT_DISPOSITION, "attachment; filename={0}".Args(m_AttachmentFileName));

      work.Response.ContentType = Format.WebContentType;
      m_Image.Save(work.Response.GetDirectOutputStreamForWriting(), Format);
    }


    public void Dispose()
    {
      DisposableObject.DisposeAndNull(ref m_Image);
    }
  }


  /// <summary>
  /// Represents MVC action result that returns ROW as JSON object for WV.RecordModel(...) ctor init
  /// </summary>
  public struct ClientRecord : IActionResult
  {
    public ClientRecord(Doc doc,
                        Exception validationError,
                        string recID = null,
                        string target = null,
                        string isoLang = null,
                        Client.ModelFieldValueListLookupFunc valueListLookupFunc = null)
    {
      RecID = recID;
      Doc = doc;
      ValidationError = validationError;
      Target = target;
      IsoLang = isoLang;
      ValueListLookupFunc = valueListLookupFunc;
    }

    public ClientRecord(Doc doc,
                        Exception validationError,
                        Func<Schema.FieldDef, JsonDataMap> simpleValueListLookupFunc,
                        string recID = null,
                        string target = null,
                        string isoLang = null)
    {
      RecID = recID;
      Doc = doc;
      ValidationError = validationError;
      Target = target;
      IsoLang = isoLang;
      if (simpleValueListLookupFunc!=null)
        ValueListLookupFunc = (_sender, _row, _def, _target, _iso) => simpleValueListLookupFunc(_def);
      else
        ValueListLookupFunc = null;
    }

    public readonly string RecID;
    public readonly Doc Doc;
    public readonly Exception ValidationError;
    public readonly string Target;
    public readonly string IsoLang;
    public readonly Client.ModelFieldValueListLookupFunc ValueListLookupFunc;


    public void Execute(Controller controller, WorkContext work)
    {
      var gen = (work.Portal!=null) ? work.Portal.RecordModelGenerator
                                    : Client.RecordModelGenerator.DefaultInstance;

      work.Response.WriteJSON( gen.RowToRecordInitJSON(Doc, ValidationError, RecID, Target, IsoLang, ValueListLookupFunc) );
    }
  }


  /// <summary>
  /// Represents MVC action result that returns JSON object with options.
  /// If JSON options are not needed then just return CLR object directly from controller action without this wrapper
  /// </summary>
  public struct JSONResult : IActionResult
  {
    public JSONResult(object data, JsonWritingOptions options)
    {
      Data = data;
      Options = options;
    }

    public JSONResult(Exception error, JsonWritingOptions options)
    {
      var http = WebConsts.STATUS_500;
      var descr = WebConsts.STATUS_500_DESCRIPTION;
      if (error != null)
      {
        descr = error.Message;
        var httpError = error as HTTPStatusException;
        if (httpError != null)
        {
          http = httpError.StatusCode;
          descr = httpError.StatusDescription;
        }
      }
      Data = new { OK = false, http = http, descr = descr };
      Options = options;
    }

    public readonly object Data;
    public readonly JsonWritingOptions Options;


    public void Execute(Controller controller, WorkContext work)
    {
      work.Response.WriteJSON( Data, Options);
    }
  }

  /// <summary>
  /// Returns HTTP 404 - not found.
  /// This should be used in place of returning exceptions where needed as it is faster
  /// </summary>
  public struct Http404NotFound : IActionResult
  {
    public Http404NotFound(string descr = null)
    {
      Description = descr;
    }

    public readonly string Description;

    public void Execute(Controller controller, WorkContext work)
    {
      var txt = WebConsts.STATUS_404_DESCRIPTION;
      if (Description.IsNotNullOrWhiteSpace())
        txt += (": " + Description);
      work.Response.StatusCode = WebConsts.STATUS_404;
      work.Response.StatusDescription = txt;

      if (work.RequestedJSON)
       work.Response.WriteJSON( new {OK = false, http = WebConsts.STATUS_404, descr = txt});
      else
       work.Response.Write(txt);
    }
  }

  /// <summary>
  /// Returns HTTP 403 - forbidden
  /// This should be used in place of returning exceptions where needed as it is faster
  /// </summary>
  public struct Http403Forbidden : IActionResult
  {
    public Http403Forbidden(string descr = null)
    {
      Description = descr;
    }

    public readonly string Description;

    public void Execute(Controller controller, WorkContext work)
    {
      var txt = WebConsts.STATUS_403_DESCRIPTION;
      if (Description.IsNotNullOrWhiteSpace())
        txt += (": " + Description);
      work.Response.StatusCode = WebConsts.STATUS_403;
      work.Response.StatusDescription = txt;

      if (work.RequestedJSON)
       work.Response.WriteJSON( new {OK = false, http = WebConsts.STATUS_403, descr = txt});
      else
       work.Response.Write(txt);
    }
  }


}
