/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azos.Conf;

namespace Azos.IO.FileSystem
{

    /// <summary>
    /// Represents a directory item in a file system. This class is NOT thread-safe
    /// </summary>
    public sealed class FileSystemDirectory : FileSystemSessionItem
    {
      #region .ctor

        /// <summary>
        /// Internal method that should not be called by developers
        /// </summary>
        public FileSystemDirectory(FileSystemSession session,
                                        string parentPath,
                                        string name,
                                        IFileSystemHandle handle) :
                                   base(session, parentPath, name, handle)
        {

        }

      #endregion

      #region Properties

        /// <summary>
        /// Returns directory names contained in this directory
        /// </summary>
        public IEnumerable<string> SubDirectoryNames
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetSubDirectoryNames(this, false);
          }
        }

                /// <summary>
                /// Async version of <see cref="P:SubDirectoryNames"/>
                /// </summary>
                public Task<IEnumerable<string>> GetSubDirectoryNamesAsync()
                {
                   CheckDisposed();
                   return m_FileSystem.DoGetSubDirectoryNamesAsync(this, false);
                }

        /// <summary>
        /// Returns file names contained in this directory
        /// </summary>
        public IEnumerable<string> FileNames
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetFileNames(this, false);
          }
        }

                /// <summary>
                /// Async version of <see cref="P:FileNames"/>
                /// </summary>
                public Task<IEnumerable<string>> GetFileNamesAsync()
                {
                   CheckDisposed();
                   return m_FileSystem.DoGetFileNamesAsync(this, false);
                }

        /// <summary>
        /// Returns directory names contained in this directory and all subdirectories
        /// </summary>
        public IEnumerable<string> RecursiveSubDirectoryNames
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetSubDirectoryNames(this, true);
          }
        }

                /// <summary>
                /// Async version of <see cref="P:RecursiveSubDirectoryNames"/>
                /// </summary>
                public Task<IEnumerable<string>> GetRecursiveSubDirectoryNamesAsync()
                {
                  CheckDisposed();
                  return m_FileSystem.DoGetSubDirectoryNamesAsync(this, true);
                }

        /// <summary>
        /// Returns file names contained in this directory and all subdirectories
        /// </summary>
        public IEnumerable<string> RecursiveFileNames
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoGetFileNames(this, true);
          }
        }

                /// <summary>
                /// Async version of <see cref="P:RecursiveFileNames"/>
                /// </summary>
                public Task<IEnumerable<string>> GetRecursiveFileNamesAsync()
                {
                   CheckDisposed();
                   return m_FileSystem.DoGetFileNamesAsync(this, true);
                }

        /// <summary>
        /// Navigates to the specified path relative to this directory
        /// </summary>
        /// <param name="path">Path relative to this directory to navigate to</param>
        /// <returns>FileSystemSessionItem instance - a directory or a file</returns>
        public FileSystemSessionItem this[string path]
        {
          get
          {
            CheckDisposed();
            return m_FileSystem.DoNavigate(m_Session, m_FileSystem.CombinePaths(Path, path));
          }
        }

                /// <summary>
                /// Async version of Item(string)
                /// </summary>
                public Task<FileSystemSessionItem> GetItemAsync(string path)
                {
                  CheckDisposed();
                  return m_FileSystem.DoNavigateAsync(m_Session, m_FileSystem.CombinePaths(Path, path));
                }

      #endregion

      #region Public Sync Methods

        /// <summary>
        /// Gets file in this directory or null if it does not exist or not a file
        /// </summary>
        public FileSystemFile GetFile(string name)
        {
          return this[name] as FileSystemFile;
        }

                /// <summary>
                /// Async version of <see cref="GetFile(string)"/>
                /// </summary>
                public Task<FileSystemFile> GetFileAsync(string name)
                {
                  return GetItemAsync(name).ContinueWith(t => {
                    return t.Result as FileSystemFile;
                  }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }

        /// <summary>
        /// Gets subdirectory in this directory or null if it does not exist or not a directory
        /// </summary>
        public FileSystemDirectory GetSubDirectory(string name)
        {
          return this[name] as FileSystemDirectory;
        }

                /// <summary>
                /// Async version of <see cref="GetFile(string)"/>
                /// </summary>
                public Task<FileSystemDirectory> GetSubDirectoryAsync(string name)
                {
                  return GetItemAsync(name).ContinueWith(t => {
                    return t.Result as FileSystemDirectory;
                  }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }

        /// <summary>
        /// Creates a new file optionally pre-allocating the specified number of bytes
        /// </summary>
        public FileSystemFile CreateFile(string name, int size = 0)
        {
          CheckCanChange();
          var result = m_FileSystem.DoCreateFile(this, name, size);
          m_Modified = true;
          return result;
        }

                /// <summary>
                /// Async version of <see cref="CreateFile(string, int)"/>
                /// </summary>
                public Task<FileSystemFile> CreateFileAsync(string name, int size = 0)
                {
                  return CheckCanChangeAsync()
                    .ContinueWith(t => {CheckDisposed(); return m_FileSystem.DoCreateFileAsync(this, name, size);}, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap()
                    .ContinueWith(t => {
                      m_Modified = true;
                      return t.Result;
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }

        /// <summary>
        /// Puts local existing file into file system
        /// </summary>
        /// <param name="name">File system file name</param>
        /// <param name="localFilePath">Local system file name</param>
        /// <param name="readOnly">Indicates whether the newly created file should be readonly</param>
        /// <returns>FileSystemFile instance</returns>
        public FileSystemFile CreateFile(string name, string localFilePath, bool readOnly = false)
        {
          CheckCanChange();
          var result = m_FileSystem.DoCreateFile(this, name, localFilePath, readOnly);
          m_Modified = true;
          return result;
        }

                /// <summary>
                /// Async version of <see cref="CreateFile(string, string, bool)"/>
                /// </summary>
                public Task<FileSystemFile> CreateFileAsync(string name, string localFilePath, bool readOnly = false)
                {
                  return CheckCanChangeAsync()
                    .ContinueWith(t => { CheckDisposed(); return m_FileSystem.DoCreateFileAsync(this, name, localFilePath, readOnly); } , TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap()
                    .ContinueWith(t => {
                      m_Modified = true;
                      return t.Result;
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }


        /// <summary>
        /// Creates a directory in this directory
        /// </summary>
        public FileSystemDirectory CreateDirectory(string name)
        {
          CheckCanChange();
          var result = m_FileSystem.DoCreateDirectory(this, name);
          m_Modified = true;
          return result;
        }

                /// <summary>
                /// Async version of <see cref="CreateDirectory(string)"/>
                /// </summary>
                public Task<FileSystemDirectory> CreateDirectoryAsync(string name)
                {
                  return CheckCanChangeAsync()
                    .ContinueWith(t => { CheckDisposed(); return m_FileSystem.DoCreateDirectoryAsync(this, name); }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap()
                    .ContinueWith(t => {
                      m_Modified = true;
                      return t.Result;
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }

        [Flags]
        public enum DirCopyFlags
        {
          None = 0,
          Directories = 1,
          Files = 2,
          Security = 4,
          Metadata = 8,
          Timestamps = 16,
          Readonly = 32,
          FilesAndDirsOnly = Directories | Files,
          All = int.MaxValue
        }

        /// <summary>
        /// Performs a deep copy of this directory into another directory that may belong to a different file system.
        /// This method allows to copy directory trees between different file systems i.e. from SVN into AmazonS3 or local file system etc.
        /// </summary>
        /// <param name="target">Target directory where the files will be placed. It's name does not have to be the same as the source's name</param>
        /// <param name="flags">Copy flags that specify what to copy</param>
        /// <param name="bufferSize">Copy buffer size</param>
        /// <param name="filter">Optional filter function</param>
        /// <param name="cancel">Optional cancellation function. Return true to abort copying</param>
        public void DeepCopyTo(FileSystemDirectory target,
                                DirCopyFlags flags = DirCopyFlags.All,
                                int bufferSize = 64 * 1024,
                                Func<FileSystemSessionItem, bool> filter = null,
                                Func<FileSystemSessionItem, bool> cancel = null)
        {
            const int MAX_BUFFER = 64 * 1024 * 1024;

            if (bufferSize<=0) bufferSize = 4 * 1024;
            if (bufferSize>MAX_BUFFER) bufferSize = MAX_BUFFER;


            var buffer = new byte[bufferSize];

            deepCopyTo(target, flags, buffer, filter, cancel);
        }

                /// <summary>
                /// Async version of DeepCopyTo(...)
                /// </summary>
                public Task DeepCopyToAsync(FileSystemDirectory target,
                                              DirCopyFlags flags = DirCopyFlags.All,
                                              int bufferSize = 64 * 1024,
                                              Func<FileSystemSessionItem, bool> filter = null,
                                              Func<FileSystemSessionItem, bool> cancel = null)
                {
                  return m_FileSystem.DoDirectoryDeepCopyAsync(this, target, flags, bufferSize, filter, cancel);
                }

      #endregion

      #region .pvt


        private void deepCopyTo(FileSystemDirectory target, DirCopyFlags flags, byte[] buffer, Func<FileSystemSessionItem, bool> filter, Func<FileSystemSessionItem, bool> cancel = null)
        {
            target.CheckCanChange();

            if (flags.HasFlag(DirCopyFlags.Directories))
            {
              foreach(var sdn in this.SubDirectoryNames)
                using(var sdir = this.GetSubDirectory(sdn))
                 if (filter==null||filter(sdir))
                 {
                    if (cancel != null && cancel(sdir)) return;

                    using(var newSDir = target.CreateDirectory(sdn))
                    {
                      copyCommonAttributes(sdir, newSDir, buffer, flags);


                      if (flags.HasFlag(DirCopyFlags.Readonly) &&
                          this.FileSystem.InstanceCapabilities.SupportsReadonlyDirectories &&
                          target.FileSystem.InstanceCapabilities.SupportsReadonlyDirectories) newSDir.ReadOnly = sdir.ReadOnly;

                      sdir.deepCopyTo(newSDir, flags, buffer, filter, cancel);
                    }
                 }//if
            }

            if (flags.HasFlag(DirCopyFlags.Files))
            {
              foreach(var fn in this.FileNames)
                using(var file = this.GetFile(fn))
                  if (filter==null||filter(file))
                  {
                    if (cancel != null && cancel(file)) return;

                    using(var newFile = target.CreateFile(fn))
                    {
                      copyCommonAttributes(file, newFile, buffer, flags);

                      if (flags.HasFlag(DirCopyFlags.Readonly) &&
                          this.FileSystem.InstanceCapabilities.SupportsReadonlyFiles &&
                          target.FileSystem.InstanceCapabilities.SupportsReadonlyFiles) newFile.ReadOnly = file.ReadOnly;

                      copyStream(file.FileStream, newFile.FileStream, buffer);
                    }
                  }
            }


        }//deepCopyTo

        private void copyCommonAttributes(FileSystemSessionItem source, FileSystemSessionItem target, byte[] buffer, DirCopyFlags flags)
        {
          if (flags.HasFlag(DirCopyFlags.Security) &&
              this.FileSystem.InstanceCapabilities.SupportsSecurity &&
              target.FileSystem.InstanceCapabilities.SupportsSecurity) copyStream(source.PermissionsStream, target.PermissionsStream, buffer);

          if (flags.HasFlag(DirCopyFlags.Metadata) &&
              this.FileSystem.InstanceCapabilities.SupportsCustomMetadata &&
              target.FileSystem.InstanceCapabilities.SupportsCustomMetadata) copyStream(source.MetadataStream, target.MetadataStream, buffer);

          if (flags.HasFlag(DirCopyFlags.Timestamps))
          {
            if (this.FileSystem.InstanceCapabilities.SupportsCreationTimestamps &&
                target.FileSystem.InstanceCapabilities.SupportsCreationTimestamps) target.CreationTimestamp = source.CreationTimestamp;

            if (this.FileSystem.InstanceCapabilities.SupportsModificationTimestamps &&
                target.FileSystem.InstanceCapabilities.SupportsModificationTimestamps) target.ModificationTimestamp = source.ModificationTimestamp;

            if (this.FileSystem.InstanceCapabilities.SupportsLastAccessTimestamps &&
                target.FileSystem.InstanceCapabilities.SupportsLastAccessTimestamps) target.LastAccessTimestamp = source.LastAccessTimestamp;
          }
        }

        private void copyStream(FileSystemStream from, FileSystemStream to, byte[] buffer)
        {
           while(true)
           {
             var read = from.Read(buffer, 0, buffer.Length);
             if (read<=0) break;
             to.Write(buffer, 0, read);
           }
        }

      #endregion

    }

}
