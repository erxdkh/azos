/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Conf;
using Azos.Security;

namespace Azos.IO.FileSystem
{
    /// <summary>
    /// Provides parameters for new sessions establishment
    /// </summary>
    public class FileSystemSessionConnectParams: Collections.INamed, IConfigurable
    {
      public static TParams Make<TParams>(IConfigSectionNode node) where TParams : FileSystemSessionConnectParams
      {
        return FactoryUtils.MakeAndConfigure<TParams>(node, typeof(TParams), args: new object[]{ node });
      }

      public static TParams Make<TParams>(string connectString, string format = Configuration.CONFIG_LACONIC_FORMAT) where TParams : FileSystemSessionConnectParams
      {
        var cfg = Configuration.ProviderLoadFromString(connectString, format).Root;
        return Make<TParams>(cfg);
      }

      public FileSystemSessionConnectParams() {}
      public FileSystemSessionConnectParams(IConfigSectionNode node) { Configure(node); }
      public FileSystemSessionConnectParams(string connectString, string format = Configuration.CONFIG_LACONIC_FORMAT)
      {
        var cfg = Configuration.ProviderLoadFromString(connectString, format).Root;
        Configure(cfg);
      }

      [Config]
      public string Name {get; set;}

      public User User {get; set;}

      public IFileSystemVersion Version {get; set;}

      public virtual void Configure(IConfigSectionNode node)
      {
        ConfigAttribute.Apply(this, node);
      }
    }


    /// <summary>
    /// Represents a user-impersonated session of working with a file system. This class is NOT thread-safe
    /// </summary>
    public class FileSystemSession : DisposableObject, Collections.INamed
    {
      #region .ctor

        /// <summary>
        /// Starts new file system session
        /// </summary>
        protected internal FileSystemSession(FileSystem fs, IFileSystemHandle handle, FileSystemSessionConnectParams cParams)
        {
          if (fs==null || cParams==null)
            throw new AzosIOException(StringConsts.FS_SESSION_BAD_PARAMS_ERROR.Args(GetType().FullName));

          ValidateConnectParams(cParams);


          m_FileSystem = fs;
          m_Handle = handle;
          m_User = cParams.User ?? User.Fake;
          m_Items = new List<FileSystemSessionItem>();
          var name = cParams.Name;
          m_Name = name.IsNullOrWhiteSpace() ? "{0}.{1}".Args(m_User.Name, Guid.NewGuid()) : name;

          lock(m_FileSystem.m_Sessions)
            m_FileSystem.m_Sessions.Add( this );
        }

        protected override void Destructor()
        {
          lock(m_FileSystem.m_Sessions)
            m_FileSystem.m_Sessions.Remove( this );

          rollbackTransactionBody();

              //delete from tail not to re-alloc list
          while (m_Items.Count>0)
              m_Items[m_Items.Count-1].Dispose();

          base.Destructor();
        }
      #endregion


      #region Fields

        protected readonly string m_Name;
        protected readonly FileSystem m_FileSystem;
        protected readonly User m_User;
        protected internal readonly List<FileSystemSessionItem> m_Items;
        protected readonly IFileSystemHandle m_Handle;

        protected internal IFileSystemTransactionHandle m_TransactionHandle;
      #endregion


      #region Properties

        /// <summary>
        /// Returns session name
        /// </summary>
        public string Name { get { return m_Name;} }

        /// <summary>
        /// Returns file system handle for this session
        /// </summary>
        public IFileSystemHandle Handle { get{ return m_Handle;} }

        /// <summary>
        /// Returns file system instance that this session operates under
        /// </summary>
        public FileSystem FileSystem { get { return m_FileSystem;}}

        /// <summary>
        /// Returns user that this file system session is for
        /// </summary>
        public User User { get { return m_User; } }

        /// <summary>
        /// Returns transaction object if transaction has been started or null
        /// </summary>
        public IFileSystemTransactionHandle TransactionHandle { get { return m_TransactionHandle; } }

        /// <summary>
        /// Returns file system items initialized through this session
        /// </summary>
        public IEnumerable<FileSystemSessionItem> Items { get { return m_Items;} }


        /// <summary>
        /// Navigates to the specified path
        /// </summary>
        /// <param name="path">Path to navigate to</param>
        /// <returns>FileSystemSessionItem instance - a directory or a file or null if it does not exist</returns>
        public FileSystemSessionItem this[string path]
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoNavigate(this, path);
          }
        }

                /// <summary>
                /// Async version of <see cref="P:Item(string)"/>
                /// </summary>
                public Task<FileSystemSessionItem> GetItemAsync(string path)
                {
                  CheckDisposed();
                  return m_FileSystem.DoNavigateAsync(this, path);
                }

        /// <summary>
        /// Gets/sets version of the file system that this session works against (a changeset that session "sees")
        /// </summary>
        public virtual IFileSystemVersion Version
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetVersion( this );
          }
          set
          {
            CheckDisposed();
            m_FileSystem.DoSetVersion( this, value );
          }
        }

                /// <summary>
                /// Async version of <see cref="P:Version"/>
                /// </summary>
                public virtual Task SetFileSystemVersionAsync(IFileSystemVersion version)
                {
                  CheckDisposed();
                  return m_FileSystem.DoSetVersionAsync(this, version);
                }

        /// <summary>
        /// Returns latest version for file systems that support versioning, null otherwise
        /// </summary>
        public virtual IFileSystemVersion LatestVersion
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetLatestVersion( this );
          }
        }

                /// <summary>
                /// Async version of <see cref="P:LatestVersion"/>
                /// </summary>
                /// <returns></returns>
                public virtual Task<IFileSystemVersion> GetLatestVersionAsync()
                {
                  CheckDisposed();
                  return m_FileSystem.DoGetLatestVersionAsync(this);
                }


          /// <summary>
          /// Returns security manager that services this file system session. This may be useful in cases when file system implements
          ///  its own permission structure and user directory
          /// </summary>
          public virtual ISecurityManager SecurityManager { get { return FileSystem.App.SecurityManager; } }

          /// <summary>
          /// Returns unique sequence provider for the system or null if it is not supported
          /// </summary>
          public virtual Data.Idgen.IUniqueSequenceProvider UniqueSequenceProvider { get { return null; } }

        #endregion

      #region Public Methods

        /// <summary>
        /// Starts a transaction returning its' transaction handle object, otherwise does nothing
        /// </summary>
        public IFileSystemTransactionHandle BeginTransaction()
        {
          CheckDisposed();
          m_TransactionHandle = m_FileSystem.DoBeginTransaction( this );
          return m_TransactionHandle;
        }

                /// <summary>
                /// Async version of <see cref="BeginTransaction()"/>
                /// </summary>
                public Task<IFileSystemTransactionHandle> BeginTransactionAsync()
                {
                  CheckDisposed();
                  return m_FileSystem.DoBeginTransactionAsync(this);
                }

        /// <summary>
        /// Commits active transaction, does nothing otherwise
        /// </summary>
        public void CommitTransaction()
        {
          CheckDisposed();
          m_FileSystem.DoCommitTransaction( this );
        }

                /// <summary>
                /// Async version of <see cref="CommitTransaction()"/>
                /// </summary>
                public Task CommitTransactionAsync()
                {
                  CheckDisposed();
                  return m_FileSystem.DoCommitTransactionAsync(this);
                }


        /// <summary>
        /// Cancels active transaction changes, does nothing otherwise
        /// </summary>
        public void RollbackTransaction()
        {
           CheckDisposed();
           rollbackTransactionBody();
        }

        private void rollbackTransactionBody()
        {
          m_FileSystem.DoRollbackTransaction(this);
        }


                /// <summary>
                /// Async version of <see cref="RollbackTransaction()"/>
                /// </summary>
                public Task RollbackTransactionAsync()
                {
                  CheckDisposed();
                  return m_FileSystem.DoRollbackTransactionAsync(this);
                }

        /// <summary>
        /// Returns specified number of versions going back from the specific version. This call is thread-safe
        /// </summary>
        public virtual IEnumerable<IFileSystemVersion> GetVersions(IFileSystemVersion from, int countBack)
        {
          return Enumerable.Empty<IFileSystemVersion>();
        }

                /// <summary>
                /// Async version of <see cref="GetVersions(IFileSystemVersion, int)"/>
                /// </summary>
                public virtual Task<IEnumerable<IFileSystemVersion>> GetVersionsAsync(IFileSystemVersion from, int countBack)
                {
                  return Task.FromResult(Enumerable.Empty<IFileSystemVersion>());
                }

      #endregion

      #region Protected

        protected virtual void ValidateConnectParams(FileSystemSessionConnectParams cParams)
        {

        }

        protected internal void CheckDisposed()
        {
           this.EnsureObjectNotDisposed();
           this.m_FileSystem.EnsureObjectNotDisposed();
        }

      #endregion

    }
}
