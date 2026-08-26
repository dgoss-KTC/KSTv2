using System.Reflection;
using System.Text;
using Kst.Integrations.Qad;

namespace Kst.Integrations.Qad.Tests.ReadOnly;

/// <summary>
/// Regression coverage for S0.3-G005 (accepted S0.3 evidence,
/// <c>docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md</c> §11; declared property in
/// <c>SECURITY.md</c> / <c>AGENTS.md</c> §7: all company database access is read-only).
///
/// Repository-level invariant: <b>KST's QAD integration code must not issue mutating SQL.</b>
/// The existing 170+ QAD tests assert SQL *shape/parameterization*; nothing asserted the
/// *absence of write-verb SQL*. These tests close that gap at the repository boundary:
///
/// 1. <see cref="All_Production_Query_Builders_Emit_ReadOnly_Sql"/> — enumerates, by reflection,
///    every public static query-builder in the production <c>Kst.Integrations.Qad</c> assembly
///    (the established <c>(string Sql, DynamicParameters Parameters)</c> convention used by all
///    QAD readers, public and pure so SQL is independently testable), invokes each with
///    representative arguments, and asserts the *generated* SQL contains no mutating verb.
///    New query builders added to the assembly are covered automatically.
///
/// 2. <see cref="Production_Qad_Source_Contains_No_Mutating_Sql_Literals"/> — scans the string
///    literals of the production <c>Kst.Integrations.Qad</c> source (the two inline
///    <c>CommandText</c> statements — the session isolation-level setting and the connectivity
///    <c>SELECT 1</c> — are not reachable through the builder convention) and asserts no literal
///    contains a statement beginning with a mutating verb.
///
/// Scope limits (explicit): this is a lexical/structural check of the application-emitted SQL,
/// not a SQL parser, and it does not prove that the QAD database account is technically
/// incapable of writes — server-side grant verification is S0.7 (S0.3-G010). The checked verb
/// set covers the mutating statement categories of the accepted architecture rule (AGENTS.md §7:
/// INSERT/UPDATE/DELETE/MERGE, plus the structural DDL/DML categories TRUNCATE/DROP/ALTER/CREATE
/// and code execution EXEC/EXECUTE).
/// </summary>
public sealed class QadReadOnlySqlTests
{
    /// <summary>
    /// SQL Server statement verbs that mutate data, structure, or execute code.
    /// Deliberately does NOT include session/setting statements such as SET
    /// (<c>SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED</c> is accepted, documented,
    /// non-mutating session behavior in <c>QadConnectionFactory</c>).
    /// </summary>
    private static readonly HashSet<string> MutatingVerbs =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE",
        "TRUNCATE", "DROP", "ALTER", "CREATE",
        "EXEC", "EXECUTE"
    ];

    // ---------------------------------------------------------------
    // 1. Behavioral: generated SQL of every production query builder
    // ---------------------------------------------------------------

    [Fact]
    public void All_Production_Query_Builders_Emit_ReadOnly_Sql()
    {
        var assembly = typeof(QadConnectionFactory).Assembly;

        var builders = assembly.GetTypes()
            .Where(t => !t.IsInterface)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(IsSqlBuilder)
            .ToList();

        Assert.True(
            builders.Count >= 10,
            $"Expected the known production QAD query builders (13 at the time of writing: " +
            $"{string.Join(", ", builders.Select(b => $"{b.DeclaringType!.Name}.{b.Name}"))}) " +
            $"but discovered only {builders.Count}. If builders were intentionally consolidated, " +
            "update this guard deliberately; otherwise a query path may have left the builder convention.");

        foreach (var builder in builders)
        {
            var sql = InvokeBuilder(builder);
            var normalized = NormalizeSqlForVerbScan(sql);

            var violation = FindMutatingVerbTokens(normalized).FirstOrDefault();
            Assert.False(
                violation.Verb is not null,
                $"SECURITY REGRESSION (S0.3-G005): {builder.DeclaringType!.Name}.{builder.Name} emits " +
                $"SQL containing the mutating verb {violation.Verb} at offset {violation.Index}: " +
                $"'{Snippet(normalized, violation.Index)}'. KST's QAD integration must be read-only " +
                "(AGENTS.md §7). Review before any intentional change.");
        }
    }

    private static bool IsSqlBuilder(MethodInfo method)
    {
        // The established production convention: public static (string Sql, DynamicParameters Parameters).
        var returnType = method.ReturnType;
        return returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(ValueTuple<,>)
            && returnType.GetGenericArguments()[0] == typeof(string);
    }

    private static string InvokeBuilder(MethodInfo builder)
    {
        var args = builder.GetParameters()
            .Select(p => BuildArgument(p, builder))
            .ToArray();

        object? result;
        try
        {
            result = builder.Invoke(null, args);
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"S0.5 read-only guard: builder {builder.DeclaringType!.Name}.{builder.Name} threw while " +
                $"being invoked with representative test arguments: {ex.InnerException?.Message}", ex);
        }

        var sql = builder.ReturnType.GetField("Item1")?.GetValue(result) as string
            ?? throw new InvalidOperationException(
                $"S0.5 read-only guard: {builder.DeclaringType!.Name}.{builder.Name} did not return a SQL string.");
        return sql;
    }

    private static object? BuildArgument(ParameterInfo parameter, MethodInfo builder)
    {
        var type = parameter.ParameterType;

        if (type == typeof(string)) return "T";
        if (type == typeof(int)) return 1;
        if (type == typeof(long)) return 1L;
        if (type == typeof(short)) return (short)1;
        if (type == typeof(bool)) return false;
        if (type == typeof(double)) return 0.0d;
        if (type == typeof(float)) return 0f;
        if (type == typeof(decimal)) return 0m;
        if (type == typeof(DateTime)) return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        if (type == typeof(DateOnly)) return DateOnly.FromDateTime(new DateTime(2026, 1, 1));
        if (type == typeof(TimeOnly)) return TimeOnly.FromDateTime(new DateTime(2026, 1, 1));
        if (type == typeof(TimeSpan)) return TimeSpan.Zero;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(CancellationToken)) return CancellationToken.None;

        if (type.IsGenericType
            && type.GetGenericArguments().Length == 1
            && type.GetGenericArguments()[0] == typeof(string)
            && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return new List<string> { "T" };
        }

        throw new InvalidOperationException(
            $"S0.5 read-only guard: no representative value generator for parameter type {type} " +
            $"({parameter.Name}) of {builder.DeclaringType!.Name}.{builder.Name}. Add one here so the new " +
            "query builder is covered by the read-only SQL check.");
    }

    // ---------------------------------------------------------------
    // 2. Source-scoped: no mutating SQL statement in production literals
    // ---------------------------------------------------------------

    [Fact]
    public void Production_Qad_Source_Contains_No_Mutating_Sql_Literals()
    {
        var projectDirectory = FindQadProjectDirectory();

        var files = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = Path.GetRelativePath(projectDirectory, path).Split(Path.DirectorySeparatorChar);
                return relative.All(segment => segment is not ("obj" or "bin"));
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            files.Count >= 15,
            $"Expected the production Kst.Integrations.Qad source tree (17 files at the time of writing) " +
            $"under {projectDirectory} but found only {files.Count}; the source scan may be pointed at the wrong directory.");

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var literals = ExtractStringLiterals(source).ToList();

            for (var i = 0; i < literals.Count; i++)
            {
                AssertNoMutatingStatementStart(literals[i], file);
            }
        }
    }

    private static void AssertNoMutatingStatementStart(string literal, string file)
    {
        var normalized = NormalizeSqlForVerbScan(literal);

        foreach (var rawStatement in normalized.Split(';'))
        {
            var statement = rawStatement.Trim();
            if (statement.Length == 0)
                continue;

            var tokenEnd = 0;
            while (tokenEnd < statement.Length
                   && (char.IsLetter(statement[tokenEnd]) || statement[tokenEnd] == '_'))
            {
                tokenEnd++;
            }

            var firstToken = statement[..tokenEnd].ToUpperInvariant();
            Assert.False(
                MutatingVerbs.Contains(firstToken),
                $"SECURITY REGRESSION (S0.3-G005): {file} contains a string literal with a statement " +
                $"starting with the mutating verb {firstToken}: '{Snippet(statement, 0)}'. KST's QAD " +
                "integration must not issue mutating SQL (AGENTS.md §7). Review before any intentional change.");
        }
    }

    private static string FindQadProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 8 && directory is not null; depth++)
        {
            var candidate = Path.Combine(directory.FullName, "Kst.Integrations.Qad", "Kst.Integrations.Qad.csproj");
            if (File.Exists(candidate))
                return Path.Combine(directory.FullName, "Kst.Integrations.Qad");
            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"S0.5 read-only guard: could not locate the Kst.Integrations.Qad project directory by walking up " +
            $"from {AppContext.BaseDirectory}.");
    }

    // ---------------------------------------------------------------
    // SQL normalization + verb scanning (shared by both tests)
    // ---------------------------------------------------------------

    /// <summary>
    /// Removes the parts of a SQL text that cannot carry a statement: block comments, line
    /// comments, string literal contents, and bracketed identifiers — so the verb scan matches
    /// statement/identifier tokens only (a verb inside a comment, literal, or identifier such as
    /// <c>[update date]</c> is not a mutation).
    /// </summary>
    private static string NormalizeSqlForVerbScan(string sql)
    {
        var result = new StringBuilder(sql.Length);
        var i = 0;
        while (i < sql.Length)
        {
            var c = sql[i];

            if (c == '\'')
            {
                i++;
                while (i < sql.Length && sql[i] != '\'')
                    i++;
                i++; // closing quote (or past end for an unterminated literal)
                result.Append("''");
                continue;
            }

            if (c == '[')
            {
                i++;
                while (i < sql.Length && sql[i] != ']')
                    i++;
                i++; // closing bracket (or past end)
                continue;
            }

            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                    i++;
                continue;
            }

            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                    i++;
                i += 2;
                continue;
            }

            result.Append(c);
            i++;
        }

        return result.ToString();
    }

    private static IEnumerable<(string? Verb, int Index)> FindMutatingVerbTokens(string normalizedSql)
    {
        var i = 0;
        while (i < normalizedSql.Length)
        {
            var c = normalizedSql[i];
            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < normalizedSql.Length
                       && (char.IsLetterOrDigit(normalizedSql[i]) || normalizedSql[i] == '_'))
                {
                    i++;
                }

                var token = normalizedSql[start..i].ToUpperInvariant();
                if (MutatingVerbs.Contains(token))
                    yield return (token, start);
            }
            else
            {
                i++;
            }
        }
    }

    private static string Snippet(string text, int index)
    {
        var start = Math.Max(0, index - 30);
        var end = Math.Min(text.Length, index + 60);
        var snippet = text[start..end].ReplaceLineEndings(" ");
        return (start > 0 ? "…" : "") + snippet + (end < text.Length ? "…" : "");
    }

    // ---------------------------------------------------------------
    // C# string-literal extraction (test 2 only)
    // ---------------------------------------------------------------

    /// <summary>
    /// Extracts the contents of C# string literals (regular, verbatim <c>@</c>, raw <c>"""</c>,
    /// with any <c>$</c>/<c>@</c> interpolation prefixes) from source text. Comments and char
    /// literals are skipped. Small lexical scanner — deliberately not a full C# parser.
    /// </summary>
    private static IEnumerable<string> ExtractStringLiterals(string source)
    {
        var literals = new List<string>();
        var i = 0;
        var n = source.Length;

        while (i < n)
        {
            var c = source[i];

            if (c == '/' && i + 1 < n && source[i + 1] == '/')
            {
                while (i < n && source[i] != '\n')
                    i++;
                continue;
            }

            if (c == '/' && i + 1 < n && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < n && !(source[i] == '*' && source[i + 1] == '/'))
                    i++;
                i += 2;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                var prefixStart = i;
                while (prefixStart > 0 && (source[prefixStart - 1] == '$' || source[prefixStart - 1] == '@'))
                    prefixStart--;
                var prefix = source[prefixStart..i];

                var (literal, next) = ReadLiteral(source, i, c, prefix);
                if (literal is not null)
                    literals.Add(literal);
                i = Math.Min(next, n);
                continue;
            }

            i++;
        }

        return literals;
    }

    private static (string? Literal, int Next) ReadLiteral(string s, int start, char quote, string prefix)
    {
        // Char literal: skip, not a SQL-bearing string.
        if (quote == '\'')
        {
            var i = start + 1;
            while (i < s.Length && s[i] != '\'')
            {
                if (s[i] == '\\')
                    i++;
                i++;
            }
            return (null, i + 1);
        }

        // Raw string: """ ... """
        if (start + 2 < s.Length && s[start + 1] == '"' && s[start + 2] == '"')
        {
            var i = start + 3;
            var contentStart = i;
            while (i + 2 < s.Length && !(s[i] == '"' && s[i + 1] == '"' && s[i + 2] == '"'))
                i++;
            return (s[contentStart..i], i + 3);
        }

        // Verbatim string: @" ... " with "" as the escaped quote.
        if (prefix.Contains('@'))
        {
            var i = start + 1;
            while (i < s.Length)
            {
                if (s[i] == '"')
                {
                    if (i + 1 < s.Length && s[i + 1] == '"')
                    {
                        i += 2;
                        continue;
                    }
                    break;
                }
                i++;
            }
            return (s[(start + 1)..i], i + 1);
        }

        // Regular string: " ... " with backslash escapes.
        var j = start + 1;
        while (j < s.Length && s[j] != '"')
        {
            if (s[j] == '\\')
                j++;
            j++;
        }
        return (s[(start + 1)..j], j + 1);
    }
}
