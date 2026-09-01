using System.Runtime.InteropServices;

namespace FullSpectrum.Knowledge.Storage;

internal sealed class SqliteDatabase : IDisposable
{
    private readonly object gate = new();
    private IntPtr handle;

    internal SqliteDatabase(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var result = NativeSqlite.sqlite3_open_v2(
            path,
            out handle,
            NativeSqlite.OpenReadWrite | NativeSqlite.OpenCreate | NativeSqlite.OpenFullMutex,
            IntPtr.Zero);
        if (result != NativeSqlite.Ok)
        {
            var message = handle == IntPtr.Zero ? $"SQLite open failed ({result})." : Error();
            Dispose();
            throw new InvalidOperationException(message);
        }
        ExecuteScript("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL;");
    }

    internal void ExecuteScript(string sql)
    {
        lock (gate)
        {
            EnsureOpen();
            var result = NativeSqlite.sqlite3_exec(handle, sql, IntPtr.Zero, IntPtr.Zero, out var error);
            if (result == NativeSqlite.Ok) return;
            var message = error == IntPtr.Zero ? Error() : Marshal.PtrToStringUTF8(error) ?? Error();
            if (error != IntPtr.Zero) NativeSqlite.sqlite3_free(error);
            throw new InvalidOperationException(message);
        }
    }

    internal int Execute(string sql, params object?[] parameters)
    {
        lock (gate)
        {
            using var statement = Prepare(sql, parameters);
            var result = NativeSqlite.sqlite3_step(statement.Handle);
            if (result != NativeSqlite.Done) throw new InvalidOperationException(Error());
            return NativeSqlite.sqlite3_changes(handle);
        }
    }

    internal long Insert(string sql, params object?[] parameters)
    {
        Execute(sql, parameters);
        lock (gate) return NativeSqlite.sqlite3_last_insert_rowid(handle);
    }

    internal IReadOnlyList<T> Query<T>(string sql, Func<SqliteRow, T> projector, params object?[] parameters)
    {
        lock (gate)
        {
            using var statement = Prepare(sql, parameters);
            var rows = new List<T>();
            while (true)
            {
                var result = NativeSqlite.sqlite3_step(statement.Handle);
                if (result == NativeSqlite.Done) return rows;
                if (result != NativeSqlite.Row) throw new InvalidOperationException(Error());
                rows.Add(projector(new SqliteRow(statement.Handle)));
            }
        }
    }

    internal T Transaction<T>(Func<T> action)
    {
        lock (gate)
        {
            ExecuteScript("BEGIN IMMEDIATE;");
            try
            {
                var result = action();
                ExecuteScript("COMMIT;");
                return result;
            }
            catch
            {
                ExecuteScript("ROLLBACK;");
                throw;
            }
        }
    }

    private SqliteStatement Prepare(string sql, object?[] parameters)
    {
        EnsureOpen();
        var result = NativeSqlite.sqlite3_prepare_v2(handle, sql, -1, out var statement, IntPtr.Zero);
        if (result != NativeSqlite.Ok) throw new InvalidOperationException(Error());
        var wrapper = new SqliteStatement(statement);
        try
        {
            for (var index = 0; index < parameters.Length; index++) Bind(statement, index + 1, parameters[index]);
            return wrapper;
        }
        catch
        {
            wrapper.Dispose();
            throw;
        }
    }

    private void Bind(IntPtr statement, int index, object? value)
    {
        var result = value switch
        {
            null => NativeSqlite.sqlite3_bind_null(statement, index),
            long number => NativeSqlite.sqlite3_bind_int64(statement, index, number),
            int number => NativeSqlite.sqlite3_bind_int64(statement, index, number),
            _ => NativeSqlite.sqlite3_bind_text(
                statement,
                index,
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
                -1,
                NativeSqlite.Transient)
        };
        if (result != NativeSqlite.Ok) throw new InvalidOperationException(Error());
    }

    private string Error() => Marshal.PtrToStringUTF8(NativeSqlite.sqlite3_errmsg(handle)) ?? "SQLite error.";
    private void EnsureOpen() { if (handle == IntPtr.Zero) throw new ObjectDisposedException(nameof(SqliteDatabase)); }

    public void Dispose()
    {
        lock (gate)
        {
            if (handle == IntPtr.Zero) return;
            var database = handle;
            var result = NativeSqlite.sqlite3_close_v2(database);
            if (result != NativeSqlite.Ok)
            {
                throw new InvalidOperationException($"SQLite close failed ({result}).");
            }
            handle = IntPtr.Zero;
        }
    }

    private sealed class SqliteStatement(IntPtr initialHandle) : IDisposable
    {
        private IntPtr handle = initialHandle;

        internal IntPtr Handle => handle;

        public void Dispose()
        {
            var statement = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (statement != IntPtr.Zero) _ = NativeSqlite.sqlite3_finalize(statement);
        }
    }
}

internal readonly struct SqliteRow(IntPtr statement)
{
    internal string Text(int index) =>
        Marshal.PtrToStringUTF8(NativeSqlite.sqlite3_column_text(statement, index))
        ?? throw new InvalidOperationException($"Column {index} is null.");

    internal string? NullableText(int index) =>
        NativeSqlite.sqlite3_column_type(statement, index) == 5
            ? null
            : Marshal.PtrToStringUTF8(NativeSqlite.sqlite3_column_text(statement, index));

    internal long Int64(int index) => NativeSqlite.sqlite3_column_int64(statement, index);
}
