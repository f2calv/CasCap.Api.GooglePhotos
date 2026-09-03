---
description: 'C# and .NET conventions for style, documentation, logging, async code and performance.'
applyTo: '**/*.cs'
---

# C# and .NET

## Style

- Follow `.editorconfig` for formatting and analyzer rules. Use 4-space indentation, LF line endings, and a final newline.
- Keep one top-level class, record, struct, or interface per file, named after the type. Nested types are allowed.
- Use `_Enums.cs` for a project's grouped enums. An underscore-prefixed options or configuration file may group one root options type with tightly coupled configuration types.
- Encode generic type parameters in filenames with braces, such as `Foo{T}.cs` and `Bar{T,U}.cs`.
- Prefix interfaces with `I` and place them in an `Abstractions` folder and namespace.
- Use file-scoped namespaces and place using directives above the namespace.
- Sort regular and global using directives alphabetically, without a separate `System` group.
- Name a project's global-usings file `GlobalUsings.cs` and keep it at the project root.
- Prefer `var` when the assigned expression makes the type clear.
- Use Allman braces. Omit braces for a single-statement `if`, `else`, `foreach`, `for`, `while`, or `using` body when readability is preserved.
- Prefer expression-bodied properties, accessors, and single-expression methods. Do not use expression bodies for constructors, operators, or local functions.
- Give explicit interface properties accessor blocks and `/// <inheritdoc/>` documentation.
- Prefer primary constructors. Use their parameters directly instead of copying them to fields unless an abstract base class must expose a protected dependency.
- Read `IOptions<T>` and `IOptionsMonitor<T>` through `.Value` or `.CurrentValue` at the point of use rather than caching the options value.
- Order dependency-injection parameters as logger, options, then application services. Use the `Svc` suffix for service parameters and fields.
- Wrap long parameter lists one parameter per line, with the closing parenthesis on its own line.
- Prefer records with `init` properties when value equality is useful.
- Mark concrete classes `sealed` unless they are designed and documented for inheritance.
- Put standard overrides such as `ToString`, `GetHashCode`, and `Equals` at the bottom of the type, before private helper regions.
- Separate public property declarations with blank lines. Keep private backing fields together without blank lines.
- Use state-oriented boolean names, such as `AuthenticationEnabled`, rather than imperative names such as `EnableAuthentication`.
- Avoid repeated magic strings. Extract well-known keys and identifiers to constants, using `nameof()` when the serialized value should track the symbol.
- Use enums for closed sets contained within the library. Prefer string constants for values that cross configuration, JSON, REST, or package boundaries.
- Apply suitable data-annotation validation to options bound from configuration, including `[Required]`, `[Url]`, `[EmailAddress]`, `[MinLength]`, and `[Range]`.

## Async Code

- Propagate `CancellationToken` through HTTP, stream, file, and paging operations.
- Await asynchronous calls unless a method is a thin pass-through that returns the task directly.
- Drop `async` and `await` from a wrapper that performs no additional work, resource disposal, or exception handling.
- Use `Task` or `Task<T>` for operations that normally perform asynchronous I/O.
- Use `ValueTask` or `ValueTask<T>` only when an operation frequently completes synchronously.
- Never cache, await twice, or concurrently await a `ValueTask`; convert it with `.AsTask()` when multiple consumers require a task.
- Library code that does not require a synchronization context must use `ConfigureAwait(false)` on awaited operations.

## XML Documentation

- Document every public or internal class, record, method, property, and enum member.
- Test methods do not require XML comments, but test classes, records, and properties do.
- Document contracts fully on interfaces and use `/// <inheritdoc/>` on implementations.
- Reference .NET and library types with `<see cref="Fully.Qualified.TypeName" />` rather than plain text.
- Preserve useful external hyperlinks when refactoring comments. Move them to `<remarks>` with `<see href="..." />`.
- Keep `<summary>` to one or two short sentences. Put defaults, implementation details, usage notes, and examples in `<remarks>`.
- Collapse summaries and remarks to one line when they remain readable at roughly 120 characters or fewer.

## Logging

- Include `{ClassName}` as the first structured template parameter and pass `nameof(EnclosingClass)` as its value.
- Use PascalCase template parameter names and do not quote placeholders.
- Name placeholders after the property being logged; do not include `.Value` or add a `Val` suffix for options values.
- Pass symbol names through `nameof()` when the symbol itself is useful data. Do not add label-only structured fields.
- Never log OAuth client secrets, access tokens, refresh tokens, authorization headers, cached token contents, Google account identifiers, or personally identifying media filenames.
- Use source-generated `[LoggerMessage]` methods on hot paths such as paging loops, uploads, downloads, and stream processing.

## HTTP and Stream Handling

- Reuse `HttpClient` instances supplied by dependency injection. Do not construct a client per request.
- Use streaming APIs for media uploads and downloads. Do not buffer complete media files unless the public method explicitly returns a byte array.
- Dispose `HttpRequestMessage`, `HttpResponseMessage`, streams, and other owned disposable resources deterministically.
- Preserve response-body context when throwing a domain exception, but redact credentials and user data from exception messages.
- Check success status and validate response content before deserializing it.

## Performance

- Use `FrozenDictionary<TKey, TValue>` or `FrozenSet<T>` for collections populated once and read repeatedly.
- Prefer `System.Threading.Lock` over `object` for dedicated lock instances.
- Parse raw UTF-8 data from `ReadOnlySpan<byte>` when practical instead of first allocating a string.
- Use static `SearchValues<char>` or `SearchValues<byte>` for repeated searches over a fixed delimiter set.
- Avoid allocating arrays, strings, and logging argument boxes inside paging, upload, download, and stream-processing loops.
- Apply `[MethodImpl(MethodImplOptions.AggressiveInlining)]` only to small leaf methods demonstrated to be hot.

## Disposable Resources

- Dispose service providers created in tests with `using` or `await using`.
- Make test helper classes static when they have no instance state.
- Avoid shared mutable static state in fixtures; each test must be independently repeatable.

## Multi-Targeting

- Guard APIs unavailable on lower target frameworks with target-framework preprocessor symbols such as `#if NET8_0_OR_GREATER`.