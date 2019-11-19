using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    public abstract class OdbBackendWritePack : IDisposable
    {
        private IntPtr nativePointer;

        protected OdbBackendWritePack(OdbBackend backend)
        {
            this.Backend = backend;
        }

        internal OdbBackend Backend { get; private set; }

        internal IndexerHandle IndexerHandle { get; set; }

        internal IntPtr BackendWritePackPointer
        {
            get
            {
                if (nativePointer == IntPtr.Zero)
                {
                    var nativeWritePack = new GitOdbBackendWritePack()
                    {
                        Append = BackendWritePackEntryPoints.AppendCallback,
                        Commit = BackendWritePackEntryPoints.CommitCallback,
                        Free = BackendWritePackEntryPoints.FreeCallback
                    };

                    nativeWritePack.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeWritePack));
                    Marshal.StructureToPtr(nativeWritePack, nativePointer, false);
                }

                return nativePointer;
            }
        }

        public virtual void Dispose()
        {
            if (IndexerHandle != null)
            {
                IndexerHandle.Dispose();
                IndexerHandle = null;
            }

            if (nativePointer != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitOdbBackendWritePack.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativePointer);
                nativePointer = IntPtr.Zero;
            }
        }

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

                Proxy.git_indexer_append(writePack.IndexerHandle, data, size, stats);

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

                Proxy.git_indexer_commit(writePack.IndexerHandle, stats);
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
