using LibGit2Sharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    public interface IConfigBackend
    {
        string Get(string key);

        IEnumerable<string> Iterate();
    }

    public interface IReadWriteConfigBackend : IConfigBackend, IDisposable
    {
        void Set(string key, string value);

        void SetMultiVar(string name, string regexp, string value);

        void Del(string key);

        void Lock();

        void Unlock(bool success);

        IConfigBackend Snapshot();
    }

    public class ConfigBackendException : LibGit2SharpException
    {
        private ConfigBackendException(GitErrorCode code, string message)
            : base(message)
        {
            Code = code;
        }

        internal GitErrorCode Code { get; private set; }

        internal static int GetReturnCode(Exception ex)
        {
            Proxy.git_error_set_str(GitErrorCategory.Config, ex);
            var backendException = ex as ConfigBackendException;
            if (backendException != null)
            {
                return (int)backendException.Code;
            }

            return (int)GitErrorCode.Error;
        }
    }

    internal class ConfigBackend : IDisposable
    {
        private IntPtr nativePointer;
        private readonly bool isReadOnly;

        public ConfigBackend(IConfigBackend backend, bool isReadOnly)
        {
            Backend = backend;
            this.isReadOnly = isReadOnly;
        }

        public IConfigBackend Backend;

        public IReadWriteConfigBackend TryGetReadWrite()
        {
            return Backend as IReadWriteConfigBackend;
        }

        public IntPtr BackendPointer
        {
            get
            {
                if (nativePointer == IntPtr.Zero)
                {
                    var nativeBackend = new GitConfigBackend()
                    {
                        Open = BackendEntryPoints.OpenCallback,
                        Get = BackendEntryPoints.GetCallback,
                        Iterator = BackendEntryPoints.IteratorCallback,
                        Free = BackendEntryPoints.FreeCallback
                    };

                    if (!isReadOnly)
                    {

                    }

                    nativeBackend.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackend));
                    Marshal.StructureToPtr(nativeBackend, nativePointer, false);
                }

                return nativePointer;
            }
        }

        public void Dispose()
        {
            var rw = TryGetReadWrite();
            if (rw != null)
            {
                rw.Dispose();
            }

            if (nativePointer != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitConfigBackend.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativePointer);
                nativePointer = IntPtr.Zero;
            }
        }

        private static class BackendEntryPoints
        {
            public static readonly GitConfigBackend.open_callback OpenCallback = Open;
            public static readonly GitConfigBackend.get_callback GetCallback = Get;
            public static readonly GitConfigBackend.set_callback SetCallback = Set;
            public static readonly GitConfigBackend.set_multivar_callback SetMultiVarCallback = SetMultiVar;
            public static readonly GitConfigBackend.del_callback DelCallback = Del;
            public static readonly GitConfigBackend.del_multivar_callback DelMultiVarCallback = DelMultiVar;
            public static readonly GitConfigBackend.iterator_callback IteratorCallback = Iterator;
            public static readonly GitConfigBackend.snapshot_callback SnapshotCallback = Snapshot;
            public static readonly GitConfigBackend.lock_callback LockCallback = Lock;
            public static readonly GitConfigBackend.unlock_callback UnlockCallback = Unlock;
            public static readonly GitConfigBackend.free_callback FreeCallback = Free;

            private static int Open(IntPtr backend, uint level, IntPtr repo)
            {
                return (int)GitErrorCode.Ok;
            }

            private static int Get(IntPtr backend, string key, out GitConfigEntry value)
            {
                throw new NotImplementedException();
            }

            private static int Set(IntPtr backend, string key, string value)
            {
                throw new NotImplementedException();
            }

            private static int SetMultiVar(IntPtr backend, string name, string regexp, string value)
            {
                throw new NotImplementedException();
            }

            private static int Del(IntPtr backend, string key)
            {
                throw new NotImplementedException();
            }

            private static int DelMultiVar(IntPtr backend, string key, string regexp)
            {
                throw new NotImplementedException();
            }

            private static int Iterator(out IntPtr iterator, IntPtr backend)
            {
                throw new NotImplementedException();
            }

            private static int Snapshot(out IntPtr readonlyBackend, IntPtr backend)
            {
                throw new NotImplementedException();
            }

            private static int Lock(IntPtr backend)
            {
                throw new NotImplementedException();
            }

            private static int Unlock(IntPtr backend, bool success)
            {
                throw new NotImplementedException();
            }

            private static void Free(IntPtr backend)
            {
                throw new NotImplementedException();
            }
        }
    }
}
