using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitConfigIterator
    {
        static GitConfigIterator()
        {
            GCHandleOffset = Marshal.OffsetOf<GitConfigIterator>(nameof(GCHandle)).ToInt32();
        }

        public IntPtr Backend;
        public uint Flags;
        public next_callback Next;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int next_callback(out GitConfigEntry entry, IntPtr iterator);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr iterator);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitConfigBackend
    {
        static GitConfigBackend()
        {
            GCHandleOffset = Marshal.OffsetOf<GitConfigBackend>(nameof(GCHandle)).ToInt32();
        }

        public uint Version;

        /// <summary>
        /// True if this backend is for a snapshot.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool ReadOnly;

        public open_callback Open;
        public get_callback Get;
        public set_callback Set;
        public set_multivar_callback SetMultiVar;
        public del_callback Del;
        public iterator_callback Iterator;
        public snapshot_callback Snapshot;
        public lock_callback Lock;
        public unlock_callback Unlock;
        public free_callback Free;

        /// <summary>
        /// This field is populated by libgit2 at backend addition time, and exists for its
        /// use only. From this side of the interop, it is unreferenced.
        /// </summary>
        public IntPtr Cfg;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int open_callback(
            IntPtr backend,
            uint level,
            IntPtr repo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int get_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string key,
            out GitConfigEntry value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int set_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string key,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int set_multivar_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string regexp,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int del_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string key);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DelMultiVar(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string key,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string regexp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int iterator_callback(out IntPtr iterator, IntPtr backend);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int snapshot_callback(
            out IntPtr readonlyBackend,
            IntPtr backend);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lock_callback(IntPtr backend);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int unlock_callback(IntPtr backend, [MarshalAs(UnmanagedType.Bool)] bool success);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr backend);
    }
}
