﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.IO;
using System.Net;

using Azos.Web;
using Azos.Wave.Mvc;

namespace Azos.Wave.Cms
{
  /// <summary>
  /// Writes a CMS Content object as a file attachment into Mvc response stream.
  /// The instance is returned from the Mvc controller action method
  /// </summary>
  public sealed class ContentAttachment : IActionResult
  {
    /// <summary>
    /// Creates an Mvc action result based on CMS content with optional attachment name override
    /// </summary>
    public ContentAttachment(Content content, string attachmentName = null)
    {
      Content = content.NonNull(nameof(content));
      AttachmentName = attachmentName;
    }

    /// <summary>
    /// Creates an Mvc action result based on CMS content with binary content offset and size and with optional attachment name override
    /// </summary>
    public ContentAttachment(Content content, int binOffset, int binSize, string attachmentName = null)
    {
      Content = content.NonNull(nameof(content));
      BinaryOffset = binOffset;
      BinarySize = binSize;
      AttachmentName = attachmentName;
    }

    /// <summary>
    /// Content to attach
    /// </summary>
    public readonly Content Content;

    /// <summary>
    /// When set, overrides the attachment name specified in the content object
    /// </summary>
    public readonly String AttachmentName;

    /// <summary>
    /// When set imposes an offset on the binary content data.
    /// Zero by default
    /// </summary>
    public readonly int BinaryOffset;

    /// <summary>
    /// When set, imposes a total size limit on binary content length. If this is less then one then
    /// reads the whole content to the end
    /// </summary>
    public readonly int BinarySize;

    /// <summary>
    /// When set, adds cache control headers
    /// </summary>
    public CacheControl? Caching { get; set; }

    /// <summary>
    /// When set (default), attaches an ETag
    /// </summary>
    public bool UseETag { get; set; } = true;

    public void Execute(Controller controller, WorkContext work)
    {
      var cache = Caching;
      var useETag = UseETag;
      if (BinaryOffset > 0 || BinarySize > 0) useETag = false;


      if (useETag && !cache.HasValue)
      {
        cache = new CacheControl {
          Cacheability = CacheControl.Type.Private,
          MustRevalidate = true,
          ProxyRevalidate = true,
          NoTransform = true,
          MaxAgeSec = 0
        };
      }

      if (cache.HasValue)
        work.Response.SetCacheControlHeaders(cache.Value, true);

      if (Content.ModifyDate.HasValue)
        work.Response.Headers[HttpResponseHeader.LastModified] = WebUtils.DateTimeToHTTPLastModifiedHeaderDateTime(Content.ModifyDate.Value);


      if (useETag)
      {
        var etag = $"\"{Content.ETag}\"";
        work.Response.Headers[HttpResponseHeader.ETag] = etag;
        var clETag = work.Request.Headers["If-None-Match"];
        if (clETag.IsNotNullOrWhiteSpace())
        {
          if (clETag.EqualsOrdSenseCase(etag))
          {
            work.Response.StatusCode = WebConsts.GetRedirectStatusCode(WebConsts.RedirectCode.NotModified_304);
            work.Response.StatusDescription = WebConsts.GetRedirectStatusDescription(WebConsts.RedirectCode.NotModified_304);
            return;
          }
        }
      }

      work.Response.ContentType = Content.ContentType;

      if (Content.IsString)
        work.Response.Write(Content.StringContent);
      else
      {
        var bin = Content.BinaryContent;
        var idx = BinaryOffset <= 0 || BinaryOffset >= bin.Length ? 0 : BinaryOffset;
        var sz = BinarySize <= 0 || idx + BinarySize >= bin.Length ? bin.Length - idx : BinarySize;

        using (var ms = new MemoryStream(bin, idx, sz))
        {
          work.Response
              .WriteStream(ms,
                          attachmentName: this.AttachmentName.IsNotNullOrWhiteSpace() ? this.AttachmentName
                                                                                      : Content.AttachmentFileName);
        }
      }
    }
  }
}
