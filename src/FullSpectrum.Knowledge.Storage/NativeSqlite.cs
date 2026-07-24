using System.Reflection;
using System.Runtime.InteropServices;

namespace FullSpectrum.Knowledge.Storage;

internal static class NativeSqlite
{
    internal const int Ok = 0;
    internal const int Row = 100;
    internal const int Done = 101;
    internal const int OpenReadWrite = 0x00000002;
    internal const int OpenCreate = 0x00000004;
    internal const int OpenFullMutex = 0x00010000;
    internal static readonly IntPtr Transient = new(-1);

    static NativeSqlite() =>
        NativeLibrary.SetDllImportResolver(typeof(NativeSqlite).Assembly, Resolve);

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "sqlite3", StringComparison.Ordinal)) return IntPtr.Zero;
        var platformName = OperatingSystem.IsWindows()
            ? "winsqlite3.dll"
            : OperatingSystem.IsMacOS() ? "libsqlite3.dylib" : "libsqlite3.so.0";
        return NativeLibrary.TryLoad(platformName, assembly, searchPath, out var handle) ? handle : IntPtr.Zero;
    }

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_open_v2(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
        out IntPtr database,
        int flags,
        IntPtr vfs);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_close_v2(IntPtr database);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sqlite3_errmsg(IntPtr database);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_exec(
        IntPtr database,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string sql,
        IntPtr callback,
        IntPtr callbackArgument,
        out IntPtr errorMessage);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sqlite3_free(IntPtr pointer);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_prepare_v2(
        IntPtr database,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string sql,
        int byteCount,
        out IntPtr statement,
        IntPtr tail);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_finalize(IntPtr statement);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_step(IntPtr statement);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_bind_text(
        IntPtr statement,
        int index,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
        int byteCount,
        IntPtr destructor);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_bind_int64(IntPtr statement, int index, long value);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_bind_null(IntPtr statement, int index);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sqlite3_column_text(IntPtr statement, int index);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_column_int64(IntPtr statement, int index);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_column_type(IntPtr statement, int index);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sqlite3_changes(IntPtr database);

    [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
    internal static extern long sqlite3_last_insert_rowid(IntPtr database);
}
