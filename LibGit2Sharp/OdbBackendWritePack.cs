using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Handles receiving a Git pack, indexing it and committing it to the ODB backend.
    /// </summary>
    public abstract class OdbBackendWritePack : IDisposable
    {
        private IndexerHandle indexerHandle;
        private IntPtr nativePointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OdbBackendWritePack"/> class.
        /// </summary>
        /// <param name="backend">Parent backend.</param>
        /// <param name="packPath">Directory to store pack files.</param>
        protected OdbBackendWritePack(OdbBackend backend, string packPath)
        {
            Ensure.ArgumentNotNull(backend, "backend");
            Ensure.ArgumentNotNullOrEmptyString(packPath, "packPath");

            this.Backend = backend;
            this.PackPath = packPath;
        }

        /// <summary>
        /// Backend that created this.
        /// </summary>
        protected OdbBackend Backend { get; private set; }

        /// <summary>
        /// Root directory where the .pack and .idx files (and staged libgit2 stream files) are stored.
        /// </summary>
        protected string PackPath { get; private set; }

        /// <summary>
        /// Commits the writepack stream to the backend.
        /// </summary>
        /// <param name="indexerHash">Final hash of the writepack, as calculated by the indexer. Forms part of the .idx and .pack filenames.</param>
        /// <returns>Error code <see cref="OdbBackend.ReturnCode"/></returns>
        protected abstract int Commit(ObjectId indexerHash);

        /// <summary>
        /// Initializes the indexer and the WritePack backend.
        /// </summary>
        /// <param name="odbHandle">ODB handle, given to us by libgit2.</param>
        /// <returns>Pointer to the native copy of this object.</returns>
        internal IntPtr Initialize(ObjectDatabaseHandle odbHandle)
        {
            Directory.CreateDirectory(PackPath);
            indexerHandle = Proxy.git_indexer_new(PackPath, 0, odbHandle);

            var nativeWritePack = new GitOdbBackendWritePack()
            {
                Backend = Backend.GitOdbBackendPointer,
                Append = BackendWritePackEntryPoints.AppendCallback,
                Commit = BackendWritePackEntryPoints.CommitCallback,
                Free = BackendWritePackEntryPoints.FreeCallback
            };

            nativeWritePack.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
            nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeWritePack));
            Marshal.StructureToPtr(nativeWritePack, nativePointer, false);
            return nativePointer;
        }

        /// <summary>
        /// Inovked by libgit2 when this writepack is no longer needed.
        /// </summary>
        public virtual void Dispose()
        {
            if (indexerHandle != null)
            {
                indexerHandle.Dispose();
            }

            if (nativePointer != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitOdbBackendWritePack.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativePointer);
                nativePointer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Static entry points for writepack callback functions.
        /// </summary>
        private static class BackendWritePackEntryPoints
        {
            public static readonly GitOdbBackendWritePack.append_callback AppendCallback = Append;
            public static readonly GitOdbBackendWritePack.commit_callback CommitCallback = DoCommit;
            public static readonly GitOdbBackendWritePack.free_callback FreeCallback = Free;

            private static int Append(
                IntPtr writePackPtr,
                IntPtr data,
                UIntPtr size,
                IntPtr stats)
            {
                var writePack = GetWritePack(writePackPtr);
                if (writePack == null)
                {
                    return (int)GitErrorCode.Error;
                }

                Proxy.git_indexer_append(writePack.indexerHandle, data, size, stats);

                return (int)GitErrorCode.Ok;
            }

            private static int DoCommit(
                IntPtr writePackPtr,
                IntPtr stats)
            {
                var writePack = GetWritePack(writePackPtr);
                if (writePack == null)
                {
                    return (int)GitErrorCode.Error;
                }

                Proxy.git_indexer_commit(writePack.indexerHandle, stats);
                var hashId = Proxy.git_indexer_hash(writePack.indexerHandle);
                try
                {
                    var result = writePack.Commit(hashId);
                    if (result < 0)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }

                return (int)GitErrorCode.Ok;
            }

            private static void Free(IntPtr writePackPtr)
            {
                var writePack = GetWritePack(writePackPtr);
                if (writePack != null)
                {
                    writePack.Dispose();
                }
            }

            private static OdbBackendWritePack GetWritePack(IntPtr backend)
            {
                return GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackendWritePack.GCHandleOffset)).Target as OdbBackendWritePack;
            }
        }
    }
}
