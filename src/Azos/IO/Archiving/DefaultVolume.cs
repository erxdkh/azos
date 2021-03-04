﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using Azos.Security;
using Azos.Serialization.Bix;

namespace Azos.IO.Archiving
{
  /// <summary>
  /// Provides default implementation for IVolume, providing optional
  /// page compression and encryption
  /// </summary>
  public sealed class DefaultVolume : DisposableObject, IVolume
  {
    /// <summary>
    /// Denotes compression scheme based on GZIP standard algorithm configured for the faster compression execution yielding minimal compression
    /// </summary>
    public const string COMPRESSION_SCHEME_GZIP = "gzip";

    /// <summary>
    /// Denotes compression scheme based on GZIP standard algorithm configured for the slower compression execution yielding maximum compression
    /// </summary>
    public const string COMPRESSION_SCHEME_GZIP_MAX = "gzip-max";


    /// <summary>
    /// Creates a new volume
    /// </summary>
    public DefaultVolume(ICryptoManager crypto, VolumeMetadataBuilder metadataBuilder, Stream stream, bool ownsStream = true)
     : this(crypto, null, metadataBuilder, stream, ownsStream)
    {
    }


    /// <summary>
    /// Creates a new volume backed by an optional `IPageCache` implementation instance
    /// </summary>
    public DefaultVolume(ICryptoManager crypto, IPageCache cache, VolumeMetadataBuilder metadataBuilder, Stream stream, bool ownsStream = true)
    {
      m_Crypto = crypto.NonNull(nameof(crypto));
      m_Cache = cache;
      m_Stream = stream.NonNull(nameof(stream));
      (m_Stream.Length == 0).IsTrue("stream.!Empty");
      metadataBuilder.Assigned.IsTrue("meta.!Assigned");

      m_Reader = new BixReader(m_Stream);
      m_Writer = new BixWriter(m_Stream);

      m_Metadata = metadataBuilder.Built;
      writeVolumeHeader();

      ctor();
    }

    /// <summary>
    /// Mounts an existing volume
    /// </summary>
    public DefaultVolume(ICryptoManager crypto, Stream stream, bool ownsStream = true)
     : this(crypto, null, stream, ownsStream)
    {

    }

    /// <summary>
    /// Mounts an existing volume backed by an optional `IPageCache` implementation instance
    /// </summary>
    public DefaultVolume(ICryptoManager crypto, IPageCache cache, Stream stream, bool ownsStream = true)
    {
      m_Crypto = crypto.NonNull(nameof(crypto));
      m_Cache = cache;
      m_Stream = stream.NonNull(nameof(stream));
      (m_Stream.Length > 0).IsTrue("stream.Empty");

      m_Reader = new BixReader(m_Stream);
      m_Writer = new BixWriter(m_Stream);

      m_Metadata = readVolumeHeader();

      ctor();
    }

    private void ctor()
    {
      if (m_Metadata.IsEncrypted)
      {
        m_Encryption = m_Crypto.MessageProtectionAlgorithms[m_Metadata.EncryptionScheme];

        if (m_Encryption == null)
          throw new ArchivingException(StringConsts.ARCHIVE_ENCRYPTION_SCHEME_NOT_SUPPORTED_ERROR.Args(m_Metadata.EncryptionScheme));
      }

      if (m_Metadata.IsCompressed)
      {
        if (!m_Metadata.CompressionScheme.IsOneOf(COMPRESSION_SCHEME_GZIP, COMPRESSION_SCHEME_GZIP_MAX))
          throw new ArchivingException(StringConsts.ARCHIVE_COMPRESSION_SCHEME_NOT_SUPPORTED_ERROR.Args(
                     m_Metadata.CompressionScheme,
                     "`{0}`, `{1}`".Args(COMPRESSION_SCHEME_GZIP, COMPRESSION_SCHEME_GZIP_MAX)
                     ));
      }
    }

    protected override void Destructor()
    {
      m_Stream.Flush();
      if (m_OwnsStream) DisposeAndNull(ref m_Stream);
      base.Destructor();
    }

    private ICryptoManager m_Crypto;
    private bool m_OwnsStream;
    private object m_StreamLock = new object();
    private Stream m_Stream;
    private BixReader m_Reader;
    private BixWriter m_Writer;
    private IPageCache m_Cache;
    private VolumeMetadata m_Metadata;
    private int m_PageSizeBytes = Format.PAGE_DEFAULT_LEN;
    private ICryptoMessageAlgorithm m_Encryption;

    private int m_ReadErrorCount;


    /// <summary>
    /// Crypto manager used for encryption algorithm handling
    /// </summary>
    public ICryptoManager Cryptography => m_Crypto;


    /// <summary>
    /// Optional page cache or null if no caching is used
    /// </summary>
    public IPageCache Cache => m_Cache;


    /// <summary>
    /// Returns archive volume metadata. Metadata gets set only at the time of new archive creation and
    /// it can not be mutated after creation
    /// </summary>
    public VolumeMetadata Metadata => m_Metadata;

    /// <summary>
    /// Page size in bytes. This affects writing of new pages which get split once this size is exceeded
    /// </summary>
    public int PageSizeBytes
    {
      get => m_PageSizeBytes;
      set => m_PageSizeBytes = value.KeepBetween(Format.PAGE_MIN_LEN, Format.PAGE_MAX_LEN);
    }


    /// <summary>
    /// Returns the total number of read errors encountered since the last call to `ResetReadErrorCount()`
    /// </summary>
    public int ReadErrorCount => m_ReadErrorCount;

    /// <summary>
    /// Sets `ReadErrorCount` to zero
    /// </summary>
    public void ResetReadErrorCount() => m_ReadErrorCount = 0;

    /// <summary>
    /// Reads one `PageInfo` object at the specified pageId.
    /// If the pageId is not exact then scrolls to the first readable page.
    /// Returns `!PageInfo.Assigned` for the EOF.
    /// </summary>
    public PageInfo ReadPageInfo(long pageId)
    {
      EnsureObjectNotDisposed();
      pageId.IsTrue(v => v >= 0, "pageId < 0");

      //align pageId by 16 for cache lookup
      pageId = IntUtils.Align16(pageId);
      var requestedPageId = pageId;

      PageInfo info;
      int len;

      var isCache = m_Cache != null && m_Cache.Enabled;

      if (!isCache || !m_Cache.TryGet(new VolumePagePtr(Metadata.Id, requestedPageId), out info))
      {
        lock (m_StreamLock)
        {
          if (!isCache || !m_Cache.TryGet(new VolumePagePtr(Metadata.Id, requestedPageId), out info))
          {
            (pageId, info, len) = seekToNextReadablePageLocation(pageId, false);
          }
        }
      }

      return info;
    }

    /// <summary>
    /// Lazily reads sequential `PageInfo` enumeration starting from the specified pageId.
    /// </summary>
    public IEnumerable<PageInfo> ReadPageInfos(long pageId)
    {
      EnsureObjectNotDisposed();
      while(true)
      {
        var pi = ReadPageInfo(pageId);
        if (!pi.Assigned) yield break;
        yield return pi;
        pageId = pi.NextPageId;
      }
    }

    /// <summary>
    /// Fills an existing page instance with archive data performing necessary decompression/decryption
    /// when needed. Returns a positive long value with the next adjacent `pageId` or a negative
    /// value to indicate the EOF condition. This method MAY be called by multiple threads at the same time
    /// (even over the same source stream which this class accesses). The implementor MAY perform internal
    /// cache/access coordination and tries to satisfy requests in a lock-free manner
    /// </summary>
    /// <param name="pageId">
    /// Requested pageId. Note: `page.PageId` will contain an actual `pageId` which may be fast-forwarded
    /// to the next readable block relative to the requested `pageId` if `exactPageId` is not set to true.
    /// </param>
    /// <param name="page">Existing page instance to fill with data</param>
    /// <param name="exactPageId">If true then will read the page exactly from the specified address</param>
    /// <returns>
    /// Returns a positive long value with the next adjacent `pageId` or a negative
    /// value to indicate the EOF condition. Throws on decipher/decompression or if bad page id was supplied when `exactPageId=true`
    /// </returns>
    public long ReadPage(long pageId, Page page, bool exactPageId = false)
    {
      EnsureObjectNotDisposed();
      pageId.IsTrue(v => v >= 0, "pageId < 0");
      page.NonNull(nameof(page));

      var pageData = page.BeginReading(pageId);

      //align pageId by 16 for cache lookup if the pageId is not exact
      if (!exactPageId) pageId = IntUtils.Align16(pageId);

      var requestedPageId = pageId;

      PageInfo info;
      int len;

      var didFetch = false;
      var isCache = m_Cache!=null && m_Cache.Enabled;

      if (!isCache || !m_Cache.TryGet(new VolumePagePtr(Metadata.Id, requestedPageId), pageData, out info))
      {
        lock (m_StreamLock)
        {
          if (!isCache || !m_Cache.TryGet(new VolumePagePtr(Metadata.Id, requestedPageId), pageData, out info))
          {
            (pageId, info, len) = seekToNextReadablePageLocation(pageId, exactPageId);

            didFetch = pageId > 0;//vs EOF

            if (didFetch) //m_Stream is on data start
              loadFromStream(pageData, len);//possibly decipher and decompress
          }
        }
      }

      //it is possible that >1 concurrent thread will put the same page in cache.
      //this is fine as probability of this is low and the benefit of NOT adding in cache
      //under global stream lock outweighs the possibly of calling a copious put(which is harmless)
      if (didFetch && isCache)
        m_Cache.Put(new VolumePagePtr(Metadata.Id, requestedPageId), info, new ArraySegment<byte>(pageData.GetBuffer(), 0, (int)pageData.Length));

      page.EndReading(pageId, info.CreateUtc, info.App, info.Host);
      return didFetch ? info.NextPageId : -1;
    }

    /// <summary>
    /// Appends the page at the end of volume. Returns the pageId of the appended page.
    /// The implementor MAY perform internal cache/access coordination and tries to satisfy requests in lock-free manner
    /// </summary>
    public long AppendPage(Page page)
    {
      EnsureObjectNotDisposed();
      page.NonNull(nameof(page))
          .Ensure(Page.Status.Written);

      var data = preparePageDataForWriting(page);//compression and encryption happens outside of lock

      lock(m_StreamLock)
      {
        var pageId = seekToNewPageLocation();
        writePageHeader(pageId, page);
        writeData(data);

        if (m_Cache != null && m_Cache.Enabled)
        {
          var info = new PageInfo
          {
            CreateUtc = page.CreateUtc,
            App = page.CreateApp,
            Host = page.CreateHost,
            NextPageId = seekToNewPageLocation()
          };

          m_Cache.Put(new VolumePagePtr(Metadata.Id, pageId), info, page.Data);
        }

        return pageId;
      }
    }


    private VolumeMetadata readVolumeHeader()
    {
      m_Stream.Position = 0;

      try
      {
        //Volume file header
        Format.VOLUME_HEADER.ForEach(c => Aver.IsTrue((byte)c == m_Reader.ReadByte(), "sig mismatch"));
        Aver.AreEqual(0, m_Reader.ReadByte(), "no \0x00");//null terminator
        Aver.AreEqual(0, m_Reader.ReadByte(), "no \0x00");

        //Info
        var info = m_Reader.ReadString();
        Aver.IsFalse(info.IsNullOrWhiteSpace(), "no info");

        //Json metadata
        var json = m_Reader.ReadString();
        Aver.IsFalse(info.IsNullOrWhiteSpace(), "no json");
        var meta = Data.ObjectValueConversion.AsJSONConfig(json, handling: Data.ConvertErrorHandling.Throw);

        //Pad
        for (var i = 0; i < Format.VOLUME_PAD_LEN; i++)
          Aver.AreEqual(Format.VOLUME_PAD_ASCII, m_Reader.ReadByte(), "bad pad");

        return new VolumeMetadata(meta);
      }
      catch(Exception cause)
      {
        throw new ArchivingException(StringConsts.ARCHIVE_VOLUME_HEADER_READ_ERROR.Args(GetType().Name, cause.ToMessageWithType()), cause);
      }
    }


    //the stream is guaranteed to be at 0
    private void writeVolumeHeader()
    {
      //Volume file header
      Format.VOLUME_HEADER.ForEach(c => m_Writer.Write((byte)c));
      m_Writer.Write((byte)0x00);//null terminator
      m_Writer.Write((byte)0x00);

      //Info
      m_Writer.Write($"Platform=Azos\nUri=https://github.com/azist/azos\nVolume={this.GetType().Name}\n");

      //Json Metadata
      var json = m_Metadata.Data.ToJSONString(Serialization.JSON.JsonWritingOptions.Compact);
      m_Writer.Write(json);

      //Pad
      for(var i=0; i<Format.VOLUME_PAD_LEN; i++)
        m_Writer.Write(Format.VOLUME_PAD_ASCII);
    }

    private long seekToNewPageLocation()
    {
      var result = IntUtils.Align16(m_Stream.Length);//always append at the very end
      m_Stream.Position = result;
      return result;
    }

    //-1 = eof, otherwise it keeps advancing
    private (long pageId, PageInfo info, int len) seekToNextReadablePageLocation(long pageId, bool exactPageId)
    {
      while(true)
      {
        if (!exactPageId) pageId = IntUtils.Align16(pageId);

        if (pageId >= m_Stream.Length) return (-1, new PageInfo(), -1);//eof

        m_Stream.Position = pageId;
        if (m_Stream.ReadByte() == Format.PAGE_HEADER_1 && m_Stream.ReadByte() == Format.PAGE_HEADER_2)
        {
          var third = (byte)(pageId >> 16);
          var second = (byte)(pageId >> 8);
          if (m_Stream.ReadByte() == third && m_Stream.ReadByte() == second)
          {
            try
            {
              var info = new PageInfo();
              info.PageId = pageId;
              info.CreateUtc = m_Reader.ReadUlong().FromSecondsSinceUnixEpochStart();
              info.Host = m_Reader.ReadString();
              info.App = m_Reader.ReadAtom();
              var len = (int)m_Reader.ReadUint();//uint varbit works faster

              if (len > 0 && len < Format.PAGE_MAX_BUFFER_LEN)
              {
                if (m_Stream.Position+len > m_Stream.Length) return (-1, new PageInfo(), -1);

                info.NextPageId = IntUtils.Align16(m_Stream.Position + len);
                return (pageId, info, len);
              }
            }
            catch
            {
              m_ReadErrorCount++;
            }
          }
        }

        if (exactPageId) throw new ArchivingException(StringConsts.ARCHIVE_PAGE_EXACT_ID_ERROR.Args(pageId));

        pageId = m_Stream.Position;
      }//while
    }

    private void writePageHeader(long pageId, Page page)
    {
      //PAGE-HDR
      m_Writer.Write(Format.PAGE_HEADER_1);
      m_Writer.Write(Format.PAGE_HEADER_2);

      //position: 2 bytes of pageId (3rd and 2nd)
      m_Writer.Write((byte)(pageId >> 16));
      m_Writer.Write((byte)(pageId >> 8));

      //utcCreateDate
      m_Writer.Write((ulong)page.CreateUtc.ToSecondsSinceUnixEpochStart());

      //host
      m_Writer.Write(page.CreateHost);

      //app
      m_Writer.Write(page.CreateApp);
    }


    private ArraySegment<byte> preparePageDataForWriting(Page page)//this is NOT under LOCK
    {
      var data = page.Data;

      //1 - compress
      if (m_Metadata.IsCompressed)
      {
        var tempStream = new MemoryStream(128 * 1024);//local
        tempStream.SetLength(0);
        using (var zip = new GZipStream(tempStream,
                                        m_Metadata.CompressionScheme
                                                  .EqualsOrdIgnoreCase(COMPRESSION_SCHEME_GZIP_MAX) ?
                                                    CompressionLevel.Optimal :
                                                    CompressionLevel.Fastest, true))
        {
          zip.Write(data.Array, data.Offset, data.Count);
        }
        data = new ArraySegment<byte>(tempStream.GetBuffer(), 0, (int)tempStream.Length);
      }

      //2 - encrypt
      if (m_Encryption != null)
      {
        var ciphered = m_Encryption.Protect(data);
        data = new ArraySegment<byte>(ciphered);
      }

      return data;
    }

    private void writeData(ArraySegment<byte> data)//under lock
    {
      m_Writer.Write((uint)data.Count);//len
      m_Stream.Write(data.Array, 0, data.Count);//data
    }


    private byte[] m_TempBuffer = new byte[128 * 1024];//accessed by 1 thread at a time
    private MemoryStream m_TempMemoryStream = new MemoryStream(128 * 1024);//accessed by 1 thread at a time

    //write:  1. compress ~25%  2. encrypt
    //read:  1. decrypt  2. decompress
    //returns false for premature EOF - when m_Stream did not have enough LEN bytes
    private void loadFromStream(MemoryStream pageData, int len) //called under LOCK, stream is at first raw byte[len]
    {
      //reads from m_Stream -> pageDataMemoryStream(a thread-safe copy)
      var direct = !m_Metadata.IsCompressed && !m_Metadata.IsEncrypted;
      var ms = direct ? pageData : m_TempMemoryStream;

      //read from file m_Stream
      ms.SetLength(len);
      ms.Position = 0;
      var buf = ms.GetBuffer();
      for (var total = 0; total < len;)
      {
        var got =  m_Stream.Read(buf, total, len - total);
        if (got <= 0) throw new ArchivingException(StringConsts.ARCHIVE_PREMATURE_EOF_ERROR);//Premature EOF
        total += got;
      }

      //not compressed or encrypted then re-use the buffer
      if (direct) return;

      //decrypt
      if (m_Encryption != null)
      {
        try
        {
          var deciphered = m_Encryption.Unprotect(new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length));
          if (deciphered == null) throw new SecurityException(StringConsts.ARCHIVE_PAGE_DECIPHER_INTEGRITY_ERROR);
          ms = new MemoryStream(deciphered);
        }
        catch(Exception error)
        {
          throw new ArchivingException(StringConsts.ARCHIVE_PAGE_DECIPHER_ERROR.Args(error.ToMessageWithType()), error);
        }
      }

      //decompress
      var stream = m_Metadata.IsCompressed ? (Stream)new GZipStream(ms, CompressionMode.Decompress, true) : ms;
      try
      {
        for(int cnt; (cnt = stream.Read(m_TempBuffer, 0, m_TempBuffer.Length)) > 0;)
        {
          pageData.Write(m_TempBuffer, 0, cnt);
          if (pageData.Length > Format.PAGE_MAX_BUFFER_LEN)
            throw new ArchivingException(StringConsts.ARCHIVE_PAGE_BUFFER_MAX_LENGTH_ERROR.Args(Format.PAGE_MAX_BUFFER_LEN));
        }
      }
      catch(Exception error)
      {
        if (m_Metadata.IsCompressed)
          throw new ArchivingException(StringConsts.ARCHIVE_PAGE_DECOMPRESSION_ERROR.Args(error.ToMessageWithType()), error);

        throw;
      }
      finally
      {
        if (m_Metadata.IsCompressed) stream.Dispose();
      }

    }

  }
}
