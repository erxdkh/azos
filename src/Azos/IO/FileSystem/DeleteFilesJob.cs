/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Linq;

using Azos.Log;
using Azos.Conf;
using Azos.Time;

namespace Azos.IO.FileSystem
{
  /// <summary>
  /// Represents a job(a scheduled event) which deletes FS files
  /// </summary>
  public class DeleteFilesJob : Event
  {
    private class stats
    {
      public int FileCount;
      public int DirCount;

      public int DelFileCount;
      public int DelDirCount;
    }


    public const string CONFIG_CONTENT_FS_SECTION        = "file-system";
    public const string CONFIG_FS_CONNECT_PARAMS_SECTION = "connect-params";
    public const string CONFIG_FS_ROOT_PATH_ATTR         = "root-path";


    public DeleteFilesJob(IEventTimer timer, IConfigSectionNode config) : this(timer, null, config: config)
    {

    }

    public DeleteFilesJob(
                           IEventTimer timer,
                           string name = null,
                           TimeSpan? interval = null,
                           IConfigSectionNode config = null,
                           FileSystem fs = null,
                           FileSystemSessionConnectParams fsConnectParams = null,
                           string fsRootPath = null
                         ) : base(timer, name, null, interval, config, EventBodyAsyncModel.LongRunningAsyncTask)
    {
      if (fs!=null) m_FS = fs;
      if (fsConnectParams!=null) m_FSConnectParams = fsConnectParams;
      if (fsRootPath.IsNotNullOrWhiteSpace()) m_FSRootPath  = fsRootPath;
    }


    protected override void Destructor()
    {
      base.Destructor();
      DisposableObject.DisposeAndNull(ref m_FS);
    }

    private object                         m_FSLock = new object();
    private FileSystem                     m_FS;
    private FileSystemSessionConnectParams m_FSConnectParams;
    private string                         m_FSRootPath;


    #region Props

      [Config] public bool Recurse{ get; set;}

      [Config] public string NameIncludePattern{ get; set;}
      [Config] public string NameExcludePattern{ get; set;}

      [Config] public ulong? MinSize{ get; set;}
      [Config] public ulong? MaxSize{ get; set;}


      [Config] public DateTime? LastModifyFrom{ get; set;}
      [Config] public DateTime? LastModifyTo  { get; set;}
      [Config] public int? LastModifyAgoHrs { get; set;}

      [Config] public bool LogStats  { get; set;}
      [Config] public bool DeleteEmptyDirs  { get; set;}


      /// <summary>
      /// Returns file system that serves static content for portals
      /// </summary>
      public IFileSystem FileSystem
      {
        get{ return m_FS;}
      }

      public FileSystemSessionConnectParams FileSystemConnectParams
      {
        get{ return m_FSConnectParams;}
      }

      /// <summary>
      /// Returns root path for content file system
      /// </summary>
      public string FileSystemRootPath
      {
        get{ return m_FSRootPath;}
      }

      /// <summary>
      /// Due to IO-nature this job is always executed as a long-running separate task
      /// </summary>
      public override EventBodyAsyncModel BodyAsyncModel
      {
        get
        {
          return EventBodyAsyncModel.LongRunningAsyncTask;
        }
        set{ }
      }

    #endregion

    #region Public
      public void BindFS(FileSystem fs, FileSystemSessionConnectParams fsConnectParams, string fsRootPath)
      {
        lock(m_FSLock)
        {
          m_FS = fs;
          m_FSConnectParams = fsConnectParams;
          m_FSRootPath      = fsRootPath;
        }
      }

      public override void Configure(IConfigSectionNode config)
      {
        base.Configure(config);

        if (config==null || !config.Exists) return;

        //Make File System
        var fsNode =  config[CONFIG_CONTENT_FS_SECTION];

        var fs = FactoryUtils.MakeAndConfigureComponent<FileSystem>(App, fsNode, typeof(Azos.IO.FileSystem.Local.LocalFileSystem));

        var fsPNode = fsNode[CONFIG_FS_CONNECT_PARAMS_SECTION];

        FileSystemSessionConnectParams fsc;

        if (fsPNode.Exists)
        {
          fsc = FileSystemSessionConnectParams.Make<FileSystemSessionConnectParams>(fsPNode);
        }
        else
        {
          fsc = new FileSystemSessionConnectParams(){ User = Azos.Security.User.Fake};
        }

        var fsp = fsNode.AttrByName(CONFIG_FS_ROOT_PATH_ATTR).Value;

        BindFS(fs, fsc, fsp);
      }

    #endregion


    #region Protected

      protected override void DoFire()
      {
        FileSystem fs;
        FileSystemSessionConnectParams fsc;
        string                         fsr;

        lock(m_FSLock)
        {
          fs = m_FS; fsc = m_FSConnectParams; fsr = m_FSRootPath;
        }

        if (fs==null || fsc==null || fsr.IsNullOrWhiteSpace())
        {
          WriteLog(MessageType.Warning, nameof(DoFire), "No FS");
          return;
        }

        if (fs.InstanceCapabilities.IsReadonly)
        {
          WriteLog(MessageType.Error, nameof(DoFire), "Readonly FS: "+fs.GetType().Name);
          return;
        }

        //Delete files
        using(var session = fs.StartSession(fsc))
        {
          var root = session[fsr] as FileSystemDirectory;
          if (root==null)
          {
            WriteLog(MessageType.Error, nameof(DoFire), "No FS root: {0}('{1}')".Args(fs.GetType().Name, fsr));
            return;
          }
          var stats = new stats();
          doLevel(root, stats);

          if (LogStats)
           WriteLog(MessageType.Info, nameof(DoFire), "Scanned {0} files, {1} dirs; Deleted {2} files, {3} dirs".Args(
                                                        stats.FileCount,
                                                        stats.DirCount,
                                                        stats.DelFileCount,
                                                        stats.DelDirCount));

          root.Dispose();
        }
      }

      protected override void DoHandleError(Exception error)
      {
        WriteLog(MessageType.Error, nameof(DoHandleError), "Job Fire() aborted with: "+error.ToMessageWithType(), error);
      }


      private void doLevel(FileSystemDirectory level, stats st)
      {
        try
        {
          deleteLocalFiles(level, st);
        }
        catch(Exception localError)
        {
          WriteLog(MessageType.Error, nameof(doLevel), "Deleting local files in '{0}'. Error: {1}".Args(level.Path, localError.ToMessageWithType(), localError));
        }

        if (Recurse)
        {
          var sdNames = level.SubDirectoryNames;
          foreach(var sdName in sdNames)
          {
            if (!App.Active) return;
            var subdir = level.GetSubDirectory(sdName);
            if (subdir!=null)
            {
              st.DirCount++;
              doLevel(subdir, st);
              subdir.Dispose();
            }

            if (DeleteEmptyDirs)
            {
              subdir = level.GetSubDirectory(sdName);
              if (subdir!=null)
              {
                var fcnt = subdir.FileNames.Count();
                if (fcnt==0)
                {
                  subdir.Delete();
                  st.DelDirCount++;
                }
                subdir.Dispose();
              }
            }


          }
        }
      }

      private void deleteLocalFiles(FileSystemDirectory level, stats st)
      {
        var nameIncludePattern = NameIncludePattern;
        var nameExcludePattern = NameExcludePattern;

        var canSize = level.FileSystem.InstanceCapabilities.SupportsFileSizes;
        var canModDates = level.FileSystem.InstanceCapabilities.SupportsLastAccessTimestamps;

        var min = MinSize;
        var max = MaxSize;
        var lmf = LastModifyFrom;
        var lmt = LastModifyTo;
        var lmah = LastModifyAgoHrs;
        var cutoffAgoDate = lmah.HasValue ? App.TimeSource.UTCNow.AddHours(-lmah.Value) : DateTime.MinValue;

        var fnames = level.FileNames;
        foreach(var fname in fnames)
        {
           st.FileCount++;

           if (nameIncludePattern!=null && !Azos.Text.Utils.MatchPattern(fname, nameIncludePattern)) continue;
           if (nameExcludePattern!=null && Azos.Text.Utils.MatchPattern(fname, nameExcludePattern)) continue;

           var file = level.GetFile(fname);
           if (file==null) continue;


           if (min.HasValue || max.HasValue)
           {
             if (canSize)
             {
               var size = file.Size;
               if (min.HasValue && size < min.Value) continue;
               if (max.HasValue && size > max.Value) continue;
             }
             else continue;
           }


           if (lmf.HasValue || lmt.HasValue || lmah.HasValue)
           {
             if (canModDates)
             {
               var fdt = file.ModificationTimestamp;
               if (fdt.HasValue)
               {
                 if (lmf.HasValue && fdt.Value < lmf.Value) continue;
                 if (lmt.HasValue && fdt.Value > lmt.Value) continue;

                 if (lmah.HasValue && fdt.Value > cutoffAgoDate) continue;
               }
             }
             else continue;
           }

           file.Delete();
           st.DelFileCount++;
        }
      }

    #endregion

  }
}
