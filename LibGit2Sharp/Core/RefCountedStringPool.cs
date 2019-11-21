using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Reference-counted pool for marshalling strings.
    /// This object keeps a global ref-count.
    /// It increments ref-count on every Get(), and decrements ref-count on every Free().
    /// It starts with a ref-count of 1, and disposal simply dec's the ref-count and prohibits future Get()s.
    /// </summary>
    /// <remarks>
    /// Libgit2's configuration backend defines a "free" callback on git_config_entry. Unfortunately,
    /// it keeps the strings around after their parent git_config_entry is "freed". See for example git_config_get_string:
    ///
    /// ret = get_entry(&amp;entry, cfg, name, true, GET_ALL_ERRORS);
	/// *out = !ret ? (entry->value ? entry->value : "") : NULL;
	/// git_config_entry_free(entry);
    ///
    /// Here, git_config_entry_free() is called immediately after stealing the string pointer (entry->value)!
    /// The reason this works in their code is that git_config_entries (where these entries come from) is ref-counted,
    /// and git_config_entry_free() only decrements the ref counter for all entries.
    /// The reason *that* works is because git_config_backend::open increments the ref counter on git_config_entry_free(),
    /// so even if the ref counter would ordinarily go to zero, the strings won't be freed until the *backend* is freed too
    /// (and all entries are GIT_REFCOUNT_DEC'd.)
    /// </remarks>
    internal class RefCountedStringPool : IDisposable
    {
        private readonly ConcurrentDictionary<string, IntPtr> managedToNative;
        private long refCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefCountedStringPool"/> class.
        /// IMPORTANT: You must dispose of the pool in order for the native memory to ever be freed.
        /// </summary>
        public RefCountedStringPool()
        {
            managedToNative = new ConcurrentDictionary<string, IntPtr>();
            refCount = 1;
        }

        /// <summary>
        /// Marshals the managed string to native UTF-8.
        /// IMPORTANT: You must call Release() once you're done using the string.
        /// </summary>
        /// <param name="managed">Managed string to marshal.</param>
        /// <returns>Pointer to the native copy.</returns>
        public IntPtr Get(string managed)
        {
            if (!TryAcquire())
            {
                throw new ObjectDisposedException("RefCountedStringPool");
            }

            return managedToNative.GetOrAdd(managed, m => EncodingMarshaler.FromManaged(Encoding.UTF8, m));
        }

        /// <summary>
        /// Decrements the ref-counter by 1.
        /// IMPORTANT: You must call this exactly once for each Get().
        /// </summary>
        public void Release()
        {
            if (NonNegativeDecrement(ref refCount) == 0)
            {
                Free();
            }
        }

        /// <summary>
        /// Disposes of the string pool. Technically, this is the same as doing a Release(), but
        /// having this object disposable makes the intention clearer.
        /// </summary>
        public void Dispose()
        {
            Release();
        }

        /// <summary>
        /// Tries to increment the ref counter. If the ref counter was at zero, indicating disposal,
        /// then the refcounter is eventually unchanged.
        /// </summary>
        /// <returns>True if the ref counter was incremented; otherwise, false if the pool is disposed.</returns>
        private bool TryAcquire()
        {
            if (Interlocked.Increment(ref refCount) == 0)
            {
                // If the ref-counter was at zero, 
                Release();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Frees all the native strings. Called once the ref-counter reaches zero.
        /// </summary>
        private void Free()
        {
            foreach (var value in managedToNative.Values)
            {
                EncodingMarshaler.Cleanup(value);
            }

            managedToNative.Values.Clear();
        }

        /// <summary>
        /// Performs an interlocked decrement, throwing an exception if the counter goes below zero.
        /// </summary>
        /// <param name="value">Counter to decrement.</param>
        /// <returns>Previous value of the counter.</returns>
        private static long NonNegativeDecrement(ref long value)
        {
            var previous = Interlocked.Decrement(ref value);
            if (previous <= 0)
            {
                try
                {
                    throw new InvalidOperationException("Ref counter went negative! This indicates a logic error internal to LibGit2Sharp.");
                }
                finally
                {
                    // This should eventually restore the error, so theoretically this exception is retriable.
                    // But it's probably game over if the ref counter state is corrupted anyway.
                    Interlocked.Increment(ref value);
                }
            }

            return previous;
        }
    }
}
