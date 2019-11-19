using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitOdbBackendWritePack
    {
        static GitOdbBackendWritePack()
        {
            GCHandleOffset = Marshal.OffsetOf<GitOdbBackendWritePack>(nameof(GCHandle)).ToInt32();
        }

        public IntPtr Backend;

        public append_callback Append;
        public commit_callback Commit;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int append_callback(IntPtr writepack, IntPtr data, UIntPtr size, IntPtr stats);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int commit_callback(IntPtr writepack, IntPtr stats);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr writepack);
    }
}
