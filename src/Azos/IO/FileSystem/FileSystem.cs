/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Conf;
using Azos.Collections;
using Azos.Security;

namespace Azos.IO.FileSystem
{
    /// <summary>
    /// Provides a base for various file system abstractions.
    /// FileSystem abstractions are mostly useful for working with components/classes that may need to inter-operate not only with local file system
    ///  but also with distributed systems like ApacheHDFS, SVN, GIT, or Sky Cluster Big-Transactional File System (BoltFS).
    ///  Azos library provides compatibility wrapper 'Azos.IO.FileSystem.Local.LocalFileSystem' for access to local machine file system
    ///  (which is based on this class and is implemented using a traditional System.IO.* set of classes).
    /// The FileSystem abstraction supports the following concepts: versioning, transactions, metadata, security; however it does not guarantee that
    ///  every implementation is capable of providing all of these functions. Query "GeneralCapabilities" and "InstanceCapabilities" to see what functions
    ///   are supported by a particular instance.
    /// This class is not thread-safe unless stated otherwise on method level, however multiple threads are allowed to obtain their own FileSystemSession
    ///  object via a call to StartSession() which is thread safe
    /// </summary>
    public abstract class FileSystem : ApplicationComponent,  IFileSystemImplementation
    {
      #region CONSTS

        public const string CONFIG_FILESYSTEM_SECTION = "file-system";
        public const string CONFIG_NAME_ATTR = "name";
        public const string CONFIG_DEFAULT_SESSION_CONNECT_PARAMS_SECTION = "default-session-connect-params";

       #endregion

        #region .ctor

        protected FileSystem(IApplication app) : base(app) => ctor();
        protected FileSystem(IApplicationComponent director) : base(director) => ctor();

        private void ctor()
        {
          m_Sessions = new List<FileSystemSession>();
        }

        protected override void Destructor()
        {
          var sessions = this.Sessions;//thread-safe copy
          sessions.ForEach( s => s.Dispose() );
          base.Destructor();
        }

      #endregion

        #region Fields

          [Config] protected string m_Name;
          protected internal List<FileSystemSession> m_Sessions;

          private FileSystemSessionConnectParams m_DefaultSessionConnectParams = new FileSystemSessionConnectParams();

        #endregion

        #region Properties
          public override string ComponentLogTopic => CoreConsts.IO_TOPIC;

          /// <summary>
          /// Provides name for the file system instance
          /// </summary>
          public string Name =>  m_Name.IsNullOrWhiteSpace() ? (m_Name = Guid.NewGuid().ToString()) : m_Name;


          /// <summary>
          /// Returns a list of sessions. This accessor is thread-safe
          /// </summary>
          public IEnumerable<FileSystemSession> Sessions
          {
             get{ lock(m_Sessions) return new List<FileSystemSession>(m_Sessions);}
          }

          /// <summary>
          /// Returns capabilities for this file system in general
          /// </summary>
          public abstract IFileSystemCapabilities GeneralCapabilities { get;}

          /// <summary>
          /// Returns capabilities for this file system instance, that may or may not be the same as GeneralCapabilities
          /// </summary>
          public abstract IFileSystemCapabilities InstanceCapabilities { get;}


        #endregion

        #region Public Methods

          /// <summary>
          /// Configures file system. This method is a part of lifecycle management and is intended to be called only by creating thread (not thread-safe)
          /// </summary>
          public void Configure(IConfigSectionNode node)
          {
            ConfigAttribute.Apply(this, node);
            DoConfigure(node);
          }

          /// <summary>
          /// Creates a new session for the user. This method is thread-safe, however the returned FileSystemSession object is not.
          /// Every thread must obtain its own session via this method
          /// </summary>
          public abstract FileSystemSession StartSession(FileSystemSessionConnectParams cParams = null);

          /// <summary>
          /// Combines two or more path segments joining them using primary file system path separator. This method is thread-safe
          /// </summary>
          public virtual string CombinePaths(string first, params string[] others)
          {
            var result = (first ?? string.Empty).TrimEnd();

            if (others == null) return result;

            for(var i=0; i<others.Length; i++)
            {
                var other = others[i];
                if (other==null) continue;

                other = other.TrimStart();

                if (other.Length==0) continue;

                if (result.Length>0)
                  foreach(var ps in GeneralCapabilities.PathSeparatorCharacters)
                    if (result.EndsWith( ps.ToString()))
                    {
                      result = result.Substring(0, result.Length-1);
                      break;
                    }

                foreach(var ps in GeneralCapabilities.PathSeparatorCharacters)
                  if (other.StartsWith( ps.ToString()))
                  {
                    other = other.Length>1 ? other.Substring(1) : string.Empty;
                    break;
                  }

                result += GeneralCapabilities.PathSeparatorCharacters[0];
                result += other;
                result = result.TrimEnd();
            }

            return result;
          }

          /// <summary>
          /// Returns the very root for the specified path, e.g. for windows it is a drive letter etc..
          /// </summary>
          public abstract string GetPathRoot(string fullPath);

          /// <summary>
          /// Returns a file name from full path or null if the specified path does not have a file path.
          /// Keep in mind that a file system does not necessarily have to support hierarchical directory structure
          /// </summary>
          /// <param name="fullPath">Full path</param>
          /// <returns>File name or null</returns>
          public abstract string ExtractFileName(string fullPath);

          /// <summary>
          /// Returns an array of path segments - directory names on the path. The last name may be directory of a file name.
          /// The specifics depend on particular file system support
          /// </summary>
          /// <param name="fullPath">Full path string like '/a/b/c'</param>
          /// <returns>And array of segments</returns>
          public abstract string[] SplitPathSegments(string fullPath);


          public override string ToString()
          {
            return "{0} '{1}'".Args(GetType().Name, Name);
          }

        #endregion

        #region Protected Methods

          protected FileSystemSessionConnectParams DefaultSessionConnectParams
          {
            get { return m_DefaultSessionConnectParams;}
          }

          /// <summary>
          /// Override to perform custom configuration
          /// </summary>
          protected virtual void DoConfigure(IConfigSectionNode node)
          {
            if (node != null)
            {
              var sessionSection = node[CONFIG_DEFAULT_SESSION_CONNECT_PARAMS_SECTION];
              m_DefaultSessionConnectParams = MakeSessionConfigParams(sessionSection);
            }

            ConfigAttribute.Apply(this, node);
          }

          protected virtual FileSystemSessionConnectParams MakeSessionConfigParams(IConfigSectionNode node)
          {
            return null;
          }

          /// <summary>
          /// Override in particular file systems to see if item can change, i.e.
          ///  for file systems that support versioning throw exception if item is in session
          ///   which "looks" at a sealed/closed version and can not change. This method may be called by multiple threads
          /// </summary>
          protected internal virtual void DoCheckCanChange(FileSystemSessionItem item)
          {
          }

                  /// <summary>
                  /// Async version of <see cref="DoCheckCanChange(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoCheckCanChange(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoCheckCanChange(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task DoCheckCanChangeAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask(() => DoCheckCanChange(item));
                  }

          /// <summary>
          /// Override in particular file systems that support versioning to get latest version object that this session can work with.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual IFileSystemVersion DoGetLatestVersion (FileSystemSession session)
          {
            return null;
          }

                  /// <summary>
                  /// Async version of <see cref="DoGetLatestVersion(FileSystemSession)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetLatestVersion(FileSystemSession)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetLatestVersion(FileSystemSession)"/>
                  /// </summary>
                  protected internal virtual Task<IFileSystemVersion> DoGetLatestVersionAsync (FileSystemSession session)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetLatestVersion(session));
                  }


          /// <summary>
          /// Override in particular file systems that support versioning to get version object for session.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual IFileSystemVersion DoGetVersion(FileSystemSession session)
          {
            return null;
          }

                  /// <summary>
                  /// Async version of <see cref="DoGetVersion(FileSystemSession)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetVersion(FileSystemSession)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetVersion(FileSystemSession)"/>
                  /// </summary>
                  protected internal virtual Task<IFileSystemVersion> DoGetVersionAsync (FileSystemSession session)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetVersion(session));
                  }

          /// <summary>
          /// Override in particular file systems that support versioning to set session to specific version.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual void DoSetVersion (FileSystemSession session, IFileSystemVersion version) {}

                  /// <summary>
                  /// Async version of <see cref="DoSetVersion (FileSystemSession, IFileSystemVersion)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoSetVersion (FileSystemSession, IFileSystemVersion)"/> and
                  /// returns already completed Task with result returned by <see cref="DoSetVersion (FileSystemSession, IFileSystemVersion)"/>
                  /// </summary>
                  protected internal virtual Task DoSetVersionAsync (FileSystemSession session, IFileSystemVersion version)
                  {
                    return TaskUtils.AsCompletedTask( () => DoSetVersion(session, version) );
                  }





          /// <summary>
          /// Override in particular file systems that support transactions to begin transaction in specified session.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual IFileSystemTransactionHandle DoBeginTransaction   (FileSystemSession session)
          {
            return null;
          }



                  /// <summary>
                  /// Async version of <see cref="DoBeginTransaction(FileSystemSession)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoBeginTransaction(FileSystemSession)"/> and
                  /// returns already completed Task with result returned by <see cref="DoBeginTransaction(FileSystemSession)"/>
                  /// </summary>
                  protected internal virtual Task<IFileSystemTransactionHandle> DoBeginTransactionAsync(FileSystemSession session)
                  {
                    return TaskUtils.AsCompletedTask( () => DoBeginTransaction(session) );
                  }

          /// <summary>
          /// Override in particular file systems that support transactions to commit transaction in specified session.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual void  DoCommitTransaction  (FileSystemSession session) {}

                  /// <summary>
                  /// Async version of <see cref="DoCommitTransaction(FileSystemSession)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoCommitTransaction(FileSystemSession)"/> and
                  /// returns already completed Task with result returned by <see cref="DoCommitTransaction(FileSystemSession)"/>
                  /// </summary>
                  protected internal virtual Task  DoCommitTransactionAsync(FileSystemSession session)
                  {
                    return TaskUtils.AsCompletedTask( () => DoCommitTransaction(session) );
                  }


          /// <summary>
          /// Override in particular file systems that support transactions to rollback transaction in specified session.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual void  DoRollbackTransaction(FileSystemSession session) {}

                  /// <summary>
                  /// Async version of <see cref="DoRollbackTransaction(FileSystemSession)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoRollbackTransaction(FileSystemSession)"/> and
                  /// returns already completed Task with result returned by <see cref="DoRollbackTransaction(FileSystemSession)"/>
                  /// </summary>
                  protected internal virtual Task DoRollbackTransactionAsync(FileSystemSession session)
                  {
                    return TaskUtils.AsCompletedTask( () => DoRollbackTransaction(session) );
                  }


          /// <summary>
          /// Override to refresh item state, i.e. re-fetch remote information.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual void DoRefresh(FileSystemSessionItem item) {}

                  /// <summary>
                  /// Async version of <see cref="DoRefreshAsync(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoRefreshAsync(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoRefreshAsync(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task DoRefreshAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask( () => DoRefresh(item) );
                  }


          /// <summary>
          /// Override to get subdirectory names of directory. If directory is null then root is assumed.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract IEnumerable<string> DoGetSubDirectoryNames(FileSystemDirectory directory, bool recursive);

                  /// <summary>
                  /// Async version of <see cref="DoGetSubDirectoryNames(FileSystemDirectory, bool)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetSubDirectoryNames(FileSystemDirectory, bool)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetSubDirectoryNames(FileSystemDirectory, bool)"/>
                  /// </summary>
                  protected internal virtual Task<IEnumerable<string>> DoGetSubDirectoryNamesAsync(FileSystemDirectory directory, bool recursive)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetSubDirectoryNames(directory, recursive));
                  }

          /// <summary>
          /// Override to get file names in directory. If directory is null then root is assumed.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract IEnumerable<string> DoGetFileNames(FileSystemDirectory directory, bool recursive);

                  /// <summary>
                  /// Async version of <see cref="DoGetFileNames(FileSystemDirectory, bool)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetFileNames(FileSystemDirectory, bool)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetFileNames(FileSystemDirectory, bool)"/>
                  /// </summary>
                  protected internal virtual Task<IEnumerable<string>> DoGetFileNamesAsync(FileSystemDirectory directory, bool recursive)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetFileNames(directory, recursive));
                  }


          /// <summary>
          /// Override to get file or directory from specified path. Return null if item does not exist.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemSessionItem DoNavigate(FileSystemSession session, string path);

                  /// <summary>
                  /// Async version of <see cref="DoNavigate(FileSystemSession, string)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoNavigate(FileSystemSession, string)"/> and
                  /// returns already completed Task with result returned by <see cref="DoNavigate(FileSystemSession, string)"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemSessionItem> DoNavigateAsync(FileSystemSession session, string path)
                  {
                    return TaskUtils.AsCompletedTask(() => DoNavigate(session, path));
                  }

          /// <summary>
          /// Override to rename item return true on success.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract bool DoRenameItem(FileSystemSessionItem item, string newName);

                  /// <summary>
                  /// Async version of <see cref="DoRenameItem(FileSystemSessionItem, string)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoRenameItem(FileSystemSessionItem, string)"/> and
                  /// returns already completed Task with result returned by <see cref="DoRenameItem(FileSystemSessionItem, string)"/>
                  /// </summary>
                  protected internal virtual Task<bool> DoRenameItemAsync(FileSystemSessionItem item, string newName)
                  {
                    return TaskUtils.AsCompletedTask(() => DoRenameItem(item, newName));
                  }

          /// <summary>
          /// Override to delete item.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract void DoDeleteItem(FileSystemSessionItem item);

                  /// <summary>
                  /// Async version of <see cref="DoDeleteItem(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoDeleteItem(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoDeleteItem(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task DoDeleteItemAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask(() => DoDeleteItem(item));
                  }


          /// <summary>
          /// Override to create a file.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemFile DoCreateFile(FileSystemDirectory dir, string name, int size);

                  /// <summary>
                  /// Async version of <see cref="DoCreateFile(FileSystemDirectory, string, int)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoCreateFile(FileSystemDirectory, string, int)"/> and
                  /// returns already completed Task with result returned by <see cref="DoCreateFile(FileSystemDirectory, string, int)"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemFile> DoCreateFileAsync(FileSystemDirectory dir, string name, int size)
                  {
                    return TaskUtils.AsCompletedTask(() => DoCreateFile(dir, name, size));
                  }

          /// <summary>
          /// Override to create a file from local file.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemFile DoCreateFile(FileSystemDirectory dir, string name, string localFile, bool readOnly);

                  /// <summary>
                  /// Async version of <see cref="DoCreateFile(FileSystemDirectory, string, string, bool)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoCreateFile(FileSystemDirectory, string, string, bool)"/> and
                  /// returns already completed Task with result returned by <see cref="DoCreateFile(FileSystemDirectory, string, string, bool)"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemFile> DoCreateFileAsync(FileSystemDirectory dir, string name, string localFile, bool readOnly)
                  {
                    return TaskUtils.AsCompletedTask(() => DoCreateFile(dir, name, localFile, readOnly));
                  }

          /// <summary>
          /// Override to create a directory.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemDirectory DoCreateDirectory(FileSystemDirectory dir, string name);

                  /// <summary>
                  /// Async version of <see cref="DoCreateDirectory(FileSystemDirectory, string)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoCreateDirectory(FileSystemDirectory, string)"/> and
                  /// returns already completed Task with result returned by <see cref="DoCreateDirectory(FileSystemDirectory, string)"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemDirectory> DoCreateDirectoryAsync(FileSystemDirectory dir, string name)
                  {
                    return TaskUtils.AsCompletedTask(() => DoCreateDirectory(dir, name));
                  }


          /// <summary>
          /// Implements asynchronous deep copy of folders where destination folder may belong to a different file system.
          /// The specifics of implementation may be dictated by particular file systems, i.e.: asynchronous strategies for getting file
          /// lists may depend on the particular system.
          /// </summary>
          protected internal virtual Task DoDirectoryDeepCopyAsync(FileSystemDirectory dirFrom,
                                                                   FileSystemDirectory dirTo,
                                                                   FileSystemDirectory.DirCopyFlags flags = FileSystemDirectory.DirCopyFlags.All,
                                                                   int bufferSize = 64 * 1024,
                                                                   Func<FileSystemSessionItem, bool> filter = null,
                                                                   Func<FileSystemSessionItem, bool> cancel = null)
          {
            return TaskUtils.AsCompletedTask(() => dirFrom.DeepCopyTo(dirTo, flags, bufferSize, filter, cancel));
          }


          /// <summary>
          /// Override to get the byte size of item (directory or file).
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract ulong DoGetItemSize(FileSystemSessionItem item);

                  /// <summary>
                  /// Async version of <see cref="DoGetItemSize(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetItemSize(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetItemSize(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task<ulong> DoGetItemSizeAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetItemSize(item));
                  }


          /// <summary>
          /// Override to get permissions stream for item (directory or file).
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemStream DoGetPermissionsStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction);

                  /// <summary>
                  /// Async version of <see cref="DoGetPermissionsStream(FileSystemSessionItem, Action{FileSystemStream})"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetPermissionsStream(FileSystemSessionItem, Action{FileSystemStream})"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetPermissionsStream(FileSystemSessionItem, Action{FileSystemStream})"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemStream> DoGetPermissionsStreamAsync(FileSystemSessionItem item, Action<FileSystemStream> disposeAction)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetPermissionsStream(item, disposeAction));
                  }

          /// <summary>
          /// Override to get metadata stream for item (directory or file).
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemStream DoGetMetadataStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction);

                  /// <summary>
                  /// Async version of <see cref="DoGetMetadataStream(FileSystemSessionItem, Action{FileSystemStream})"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetMetadataStream(FileSystemSessionItem, Action{FileSystemStream})"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetMetadataStream(FileSystemSessionItem, Action{FileSystemStream})"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemStream> DoGetMetadataStreamAsync(FileSystemSessionItem item, Action<FileSystemStream> disposeAction)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetMetadataStream(item, disposeAction));
                  }

          /// <summary>
          /// Override to get file stream.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract FileSystemStream DoGetFileStream(FileSystemFile file, Action<FileSystemStream> disposeAction);

                  /// <summary>
                  /// Async version of <see cref="DoGetFileStream(FileSystemFile, Action{FileSystemStream})"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetFileStream(FileSystemFile, Action{FileSystemStream})"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetFileStream(FileSystemFile, Action{FileSystemStream})"/>
                  /// </summary>
                  protected internal virtual Task<FileSystemStream> DoGetFileStreamAsync(FileSystemFile file, Action<FileSystemStream> disposeAction)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetFileStream(file, disposeAction));
                  }

          /// <summary>
          /// Override to get item creation timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract DateTime? DoGetCreationTimestamp(FileSystemSessionItem item);

          /// <summary>
          /// Override to get item modification timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract DateTime? DoGetModificationTimestamp(FileSystemSessionItem item);

          /// <summary>
          /// Override to get item last access timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract DateTime? DoGetLastAccessTimestamp(FileSystemSessionItem item);

          /// <summary>
          /// Override to set item creation timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract void DoSetCreationTimestamp(FileSystemSessionItem item, DateTime timestamp);

                  /// <summary>
                  /// Async version of <see cref="DoSetCreationTimestamp(FileSystemSessionItem, DateTime)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoSetCreationTimestamp(FileSystemSessionItem, DateTime)"/> and
                  /// returns already completed Task with result returned by <see cref="DoSetCreationTimestamp(FileSystemSessionItem, DateTime)"/>
                  /// </summary>
                  protected internal virtual Task DoSetCreationTimestampAsync(FileSystemSessionItem item, DateTime timestamp)
                  {
                    return TaskUtils.AsCompletedTask(() => DoSetCreationTimestamp(item, timestamp));
                  }

          /// <summary>
          /// Override to set item modification timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract void DoSetModificationTimestamp(FileSystemSessionItem item, DateTime timestamp);

                  /// <summary>
                  /// Async version of <see cref="DoSetModificationTimestamp(FileSystemSessionItem, DateTime)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoSetModificationTimestamp(FileSystemSessionItem, DateTime)"/> and
                  /// returns already completed Task with result returned by <see cref="DoSetModificationTimestamp(FileSystemSessionItem, DateTime)"/>
                  /// </summary>
                  protected internal virtual Task DoSetModificationTimestampAsync(FileSystemSessionItem item, DateTime timestamp)
                  {
                    return TaskUtils.AsCompletedTask(() => DoSetModificationTimestamp(item, timestamp));
                  }

          /// <summary>
          /// Override to set item last access timestamp.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract void DoSetLastAccessTimestamp(FileSystemSessionItem item, DateTime timestamp);

                  /// <summary>
                  /// Async version of <see cref="DoSetLastAccessTimestamp(FileSystemSessionItem, DateTime)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoSetLastAccessTimestamp(FileSystemSessionItem, DateTime)"/> and
                  /// returns already completed Task with result returned by <see cref="DoSetLastAccessTimestamp(FileSystemSessionItem, DateTime)"/>
                  /// </summary>
                  protected internal virtual Task DoSetLastAccessTimestampAsync(FileSystemSessionItem item, DateTime timestamp)
                  {
                    return TaskUtils.AsCompletedTask(() => DoSetLastAccessTimestamp(item, timestamp));
                  }

          /// <summary>
          /// Override to get item readonly status.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract bool DoGetReadOnly(FileSystemSessionItem item);

          /// <summary>
          /// Override to set item readonly status.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal abstract void DoSetReadOnly(FileSystemSessionItem item, bool readOnly);

                  /// <summary>
                  /// Async version of <see cref="DoSetReadOnly(FileSystemSessionItem, bool)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoSetReadOnly(FileSystemSessionItem, bool)"/> and
                  /// returns already completed Task with result returned by <see cref="DoSetReadOnly(FileSystemSessionItem, bool)"/>
                  /// </summary>
                  protected internal virtual Task DoSetReadOnlyAsync(FileSystemSessionItem item, bool readOnly)
                  {
                    return TaskUtils.AsCompletedTask(() => DoSetReadOnly(item, readOnly));
                  }


          /// <summary>
          /// Override in particular file systems to get user who created item.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual User DoGetCreationUser(FileSystemSessionItem item)
          {
            return User.Fake;
          }

                  /// <summary>
                  /// Async version of <see cref="DoGetCreationUser(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetCreationUser(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetCreationUser(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task<User> DoGetCreationUserAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetCreationUser(item));
                  }

          /// <summary>
          /// Override in particular file systems to get user who was the last user modifying the item.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual User  DoGetModificationUser(FileSystemSessionItem item)
          {
            return User.Fake;
          }

                  /// <summary>
                  /// Async version of <see cref="DoGetModificationUser(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetModificationUser(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetModificationUser(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task<User> DoGetModificationUserAsync(FileSystemSessionItem item)
                  {
                    return TaskUtils.AsCompletedTask(() => DoGetModificationUser(item));
                  }

          /// <summary>
          /// Override in particular file systems to get user who was the last user accessing the item.
          /// This method may be called by multiple threads
          /// </summary>
          protected internal virtual User  DoGetLastAccessUser(FileSystemSessionItem item)
          {
            return User.Fake;
          }

                  /// <summary>
                  /// Async version of <see cref="DoGetLastAccessUser(FileSystemSessionItem)"/>.
                  /// This base/default implementation just synchronously calls <see cref="DoGetLastAccessUser(FileSystemSessionItem)"/> and
                  /// returns already completed Task with result returned by <see cref="DoGetLastAccessUser(FileSystemSessionItem)"/>
                  /// </summary>
                  protected internal virtual Task<User> DoGetLastAccessUserAsync(FileSystemSessionItem item)
                  {
                      return TaskUtils.AsCompletedTask(() => DoGetLastAccessUser(item));
                  }



          protected internal virtual Task DoFlushAsync(FileSystemStream stream, CancellationToken cancellationToken)
          {
            return TaskUtils.AsCompletedTask(stream.Flush);
          }

          protected internal virtual Task<int> DoReadAsync(FileSystemStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
          {
            return TaskUtils.AsCompletedTask(() => stream.Read(buffer, offset, count));
          }

          protected internal virtual Task DoWriteAsync(FileSystemStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
          {
            return TaskUtils.AsCompletedTask(() => stream.Write(buffer, offset, count));
          }

        #endregion

    }


}
