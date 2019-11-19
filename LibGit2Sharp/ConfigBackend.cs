﻿using LibGit2Sharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    public abstract class ConfigBackend : IDisposable
    {
        private IntPtr nativePointer;
        private uint level;

        internal IntPtr BackendPointer
        {
            get
            {
                if (nativePointer == IntPtr.Zero)
                {
                    var nativeBackend = new GitConfigBackend()
                    {
                        Version = 1,
                        ReadOnly = false,
                        Open = BackendEntryPoints.OpenCallback,
                        Get = BackendEntryPoints.GetCallback,
                        Iterator = BackendEntryPoints.IteratorCallback,
                        Set = BackendEntryPoints.SetCallback,
                        SetMultiVar = BackendEntryPoints.SetMultiVarCallback,
                        Del = BackendEntryPoints.DelCallback,
                        Lock = BackendEntryPoints.LockCallback,
                        Unlock = BackendEntryPoints.UnlockCallback,
                        Free = BackendEntryPoints.FreeCallback
                    };

                    nativeBackend.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackend));
                    Marshal.StructureToPtr(nativeBackend, nativePointer, false);
                }

                return nativePointer;
            }
        }

        public abstract string Get(string key);

        public virtual IEnumerable<string> Iterate()
        {
            throw ConfigBackendException.NotImplemented("Iterator");
        }

        public virtual void Set(string key, string value)
        {
            throw ConfigBackendException.NotImplemented("Set");
        }

        public virtual void SetMultiVar(string name, string regexp, string value)
        {
            throw ConfigBackendException.NotImplemented("SetMultiVar");
        }

        public virtual void Del(string key)
        {
            throw ConfigBackendException.NotImplemented("Del");
        }

        public virtual void DelMultiVar(string name, string regexp)
        {
            throw ConfigBackendException.NotImplemented("DelMultiVar");
        }

        public virtual void Lock()
        {
            throw ConfigBackendException.NotImplemented("Lock");
        }

        public virtual void Unlock(bool success)
        {
            throw ConfigBackendException.NotImplemented("Unlock");
        }

        public virtual void Dispose()
        {
            if (nativePointer != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitConfigBackend.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativePointer);
                nativePointer = IntPtr.Zero;
            }
        }

        internal void Open(uint level)
        {
            this.level = level;
        }

        internal IntPtr AllocateEntry(string name, string value)
        {
            var nativeEntry = new GitConfigBackendEntry()
            {
                Name = name,
                Value = value,
                IncludeDepth = 0,
                Level = level,
                Free = BackendEntryPoints.FreeEntryCallback
            };

            nativeEntry.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(nativeEntry));
            return Marshal.AllocHGlobal(Marshal.SizeOf(nativeEntry));
        }

        public sealed class ConfigBackendException : LibGit2SharpException
        {
            private ConfigBackendException(GitErrorCode code, string message)
                : base(message)
            {
                Code = code;
            }

            internal GitErrorCode Code { get; private set; }

            public static ConfigBackendException NotImplemented(string operationName)
            {
                return new ConfigBackendException(
                    GitErrorCode.User,
                    string.Format("Operation {0} is not suppoted by this backend.", operationName));
            }

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

        private class ConfigBackendIterator : IDisposable
        {
            private readonly ConfigBackend backend;
            private readonly IEnumerator<string> keys;
            private IntPtr nativePointer;

            public ConfigBackendIterator(ConfigBackend backend, IEnumerator<string> keys)
            {
                this.backend = backend;
                this.keys = keys;
            }

            public IntPtr NativeIterator
            {
                get
                {
                    if (nativePointer == IntPtr.Zero)
                    {
                        var native = new GitConfigIterator()
                        {
                            Backend = backend.BackendPointer,
                            Free = EnumeratorEntryPoints.FreeCallback,
                            Next = EnumeratorEntryPoints.NextCallback
                        };

                        native.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                        nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(native));
                        Marshal.StructureToPtr(native, nativePointer, false);
                    }

                    return nativePointer;
                }
            }

            public bool GetNext(out IntPtr next)
            {
                next = IntPtr.Zero;
                if (keys.MoveNext())
                {
                    var value = backend.Get(keys.Current);
                    next = backend.AllocateEntry(keys.Current, value);
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (nativePointer != IntPtr.Zero)
                {
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitConfigIterator.GCHandleOffset)).Free();
                    Marshal.FreeHGlobal(nativePointer);
                    nativePointer = IntPtr.Zero;
                }
            }

            private static class EnumeratorEntryPoints
            {
                public static readonly GitConfigIterator.next_callback NextCallback = Next;

                public static readonly GitConfigIterator.free_callback FreeCallback = Free;

                private static int Next(out IntPtr entry, IntPtr iteratorPtr)
                {
                    entry = IntPtr.Zero;
                    var iterator = MarshalFromPtr(iteratorPtr);
                    if (iterator == null)
                    {
                        return (int)GitErrorCode.Error;
                    }

                    try
                    {
                        if (iterator.GetNext(out entry))
                        {
                            return (int)GitErrorCode.Ok;
                        }

                        return (int)GitErrorCode.IterOver;
                    }
                    catch (Exception ex)
                    {
                        return ConfigBackendException.GetReturnCode(ex);
                    }
                }

                private static void Free(IntPtr iterator)
                {
                    throw new NotImplementedException();
                }

                private static ConfigBackendIterator MarshalFromPtr(IntPtr backendPtr)
                {
                    var intPtr = Marshal.ReadIntPtr(backendPtr, GitConfigBackend.GCHandleOffset);
                    var iteratorBackend = GCHandle.FromIntPtr(intPtr).Target as ConfigBackendIterator;

                    if (iteratorBackend == null)
                    {
                        Proxy.git_error_set_str(GitErrorCategory.Reference, "Cannot retrieve the managed ConfigBackendIterator.");
                        return null;
                    }

                    return iteratorBackend;
                }
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
            public static readonly GitConfigBackendEntry.free_callback FreeEntryCallback = FreeEntry;

            private static int Open(IntPtr backendPtr, uint level, IntPtr repo)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                backend.Open(level);
                return (int)GitErrorCode.Ok;
            }

            private static int Get(IntPtr backendPtr, string key, out IntPtr entryPtr)
            {
                entryPtr = IntPtr.Zero;
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                string value;
                try
                {
                    value = backend.Get(key);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                entryPtr = backend.AllocateEntry(key, value);
                return (int)GitErrorCode.Ok;
            }

            private static int Set(IntPtr backendPtr, string key, string value)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.Set(key, value);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int SetMultiVar(IntPtr backendPtr, string name, string regexp, string value)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.SetMultiVar(name, regexp, value);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int Del(IntPtr backendPtr, string key)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.Del(key);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int DelMultiVar(IntPtr backendPtr, string key, string regexp)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.DelMultiVar(key, regexp);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int Iterator(out IntPtr iterator, IntPtr backendPtr)
            {
                iterator = IntPtr.Zero;
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    var enumerator = backend.Iterate().GetEnumerator();
                    var backendIterator = new ConfigBackendIterator(backend, enumerator);
                    iterator = backendIterator.NativeIterator;
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int Snapshot(out IntPtr readonlyBackend, IntPtr backendPtr)
            {
                readonlyBackend = IntPtr.Zero;
                return (int)GitErrorCode.Error;
            }

            private static int Lock(IntPtr backendPtr)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.Lock();
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static int Unlock(IntPtr backendPtr, bool success)
            {
                var backend = MarshalToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.Unlock(success);
                }
                catch (Exception ex)
                {
                    return ConfigBackendException.GetReturnCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static void Free(IntPtr backendPtr)
            {
                var backend = MarshalToBackend(backendPtr);
                backend.Dispose();
            }

            private static void FreeEntry(IntPtr entryPtr)
            {
                var intPtr = Marshal.ReadIntPtr(entryPtr, GitConfigBackendEntry.GCHandleOffset);
                GCHandle.FromIntPtr(intPtr).Free();
                Marshal.FreeHGlobal(entryPtr);
            }

            private static ConfigBackend MarshalToBackend(IntPtr backendPtr)
            {
                var intPtr = Marshal.ReadIntPtr(backendPtr, GitConfigBackend.GCHandleOffset);
                var cfgBackend = GCHandle.FromIntPtr(intPtr).Target as ConfigBackend;

                if (cfgBackend == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Reference, "Cannot retrieve the managed ConfigBackend.");
                    return null;
                }

                return cfgBackend;
            }
        }
    }
}
