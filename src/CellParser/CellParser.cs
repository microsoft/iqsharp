namespace Microsoft.Quantum.IQSharp.CellParser;

using System.Text;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;
using OneOf;
using INamespaceList = IList<(string Namespace, string? Alias)>;
using NamespaceList = List<(string Namespace, string? Alias)>;


internal static class CellParserExtensions
{
    public static string AsComments(this IEnumerable<(string Namespace, string? Alias)> openStatements) =>
        string.Join(Environment.NewLine, openStatements.Select(open =>
            $"//     open {open.Namespace}{(open.Alias is string alias ? $" as {alias}" : "")};"
        ));
}

public class CellParser
{
    public record CallableDeclaration(string Contents, string? Namespace, INamespaceList OpenNamespaces);
    public record UdtDeclaration(string Contents, string? Namespace, INamespaceList OpenNamespaces);
    public record MagicCommandInvocation(string CommandName, string Input = "");

    public sealed class CellPart : OneOfBase<CallableDeclaration, UdtDeclaration, MagicCommandInvocation>
    {
        public CellPart(OneOf<CallableDeclaration, UdtDeclaration, MagicCommandInvocation> input) : base(input)
        { }

        public static implicit operator CellPart(CallableDeclaration _) => new CellPart(_);
        public static implicit operator CellPart(UdtDeclaration _) => new CellPart(_);
        public static implicit operator CellPart(MagicCommandInvocation _) => new CellPart(_);
    }


    public record CellSplitResult(IEnumerable<CellPart> Parts, INamespaceList GloballyOpenNamespaces)
    {
        /// <summary>
        ///     Returns the results of splitting an IQ# cell as a string useful
        ///     in debugging, with each cell part listed as a separate text
        ///     snippet.
        /// </summary>
        public string ToDebugString() =>
            string.Join(
                "\n// ---\n",
                Parts.Select(part =>
                    part.Match(
                        callable => 
                            $"// Callable declaration in namespace {callable.Namespace ?? "<default snippet>"}\n" +
                            "// Open statements in effect for declaration:\n" +
                            string.Join(
                                Environment.NewLine,
                                callable.OpenNamespaces.AsComments()
                            ) +
                            Environment.NewLine +
                            callable.Contents,
                        udt => "udt",
                        magic => $"// Magic command invocation:\n{magic.CommandName} {magic.Input}"
                    )
                )
            ) +
            "\n// ---\n// Globally opened namespaces after splitting cells:\n" +
            GloballyOpenNamespaces.AsComments();
    }

    internal class Listener : IQSharpCellParserBaseListener
    {
        private readonly IList<CellPart> Parts = new List<CellPart>();

        public IEnumerable<CellPart> CellParts => Parts.ToImmutableList();

        internal INamespaceList GloballyOpenedNamespaces = new NamespaceList();
        private string? CurrentNamespace = null;
        private INamespaceList? CurrentlyOpenedNamespaces = null;

        public override void EnterCallableDeclaration(IQSharpCellParser.CallableDeclarationContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var text = context.Start.InputStream.GetText(new Interval(start, stop));

            Parts.Add(new CallableDeclaration(text, CurrentNamespace, CurrentNamespace == null ? GloballyOpenedNamespaces : CurrentlyOpenedNamespaces));
            base.EnterCallableDeclaration(context);
        }

        // FIXME: Consolidate with previous method.
        public override void EnterUserDefinedType(IQSharpCellParser.UserDefinedTypeContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var text = context.Start.InputStream.GetText(new Interval(start, stop));

            Parts.Add(new UdtDeclaration(text, CurrentNamespace, CurrentNamespace == null ? GloballyOpenedNamespaces : CurrentlyOpenedNamespaces));
            base.EnterUserDefinedType(context);
        }

        public override void EnterOpenDirective(IQSharpCellParser.OpenDirectiveContext context)
        {
            // There's either 1 or 2 qualifiedName matches, depending on whether
            // there's an alias or not.
            var openParts = context.qualifiedName();
            var nsName = openParts[0].ToDottedName();
            var alias = openParts.Length == 2 ? openParts[1].ToDottedName() : null;
            if (CurrentNamespace == null) // In global mode
            {
                GloballyOpenedNamespaces.Add((nsName, alias));
            }
            else // Inside of a namespace
            {
                System.Diagnostics.Debug.Assert(CurrentlyOpenedNamespaces != null, "Internal error: CurrentlyOpenedNamespaces was null when CurrentNamespace is not-null.");
                CurrentlyOpenedNamespaces.Add((nsName, alias));
            }
            base.EnterOpenDirective(context);
        }

        public override void EnterNamespace(IQSharpCellParser.NamespaceContext context)
        {
            CurrentNamespace = context.name.ToDottedName();
            CurrentlyOpenedNamespaces = new NamespaceList();
            base.EnterNamespace(context);
        }

        public override void ExitNamespace(IQSharpCellParser.NamespaceContext context)
        {
            CurrentNamespace = null;
            CurrentlyOpenedNamespaces = null;
            base.ExitNamespace(context);
        }
    }

    private static CellSplitResult SplitOneChunk(string cellContents, INamespaceList openNamespaces)
    {
        var charStream = CharStreams.fromString(cellContents);

        var lexer = new QSharpLexer(charStream);
        lexer.AddErrorListener((syntaxError) =>
            Console.WriteLine(syntaxError)
        );

        var tokens = new CommonTokenStream(lexer);
        var parser = new IQSharpCellParser(tokens);
        parser.AddErrorListener((syntaxError) =>
            Console.WriteLine(syntaxError)
        );

        var walker = new ParseTreeWalker();
        var listener = new Listener
        {
            GloballyOpenedNamespaces = openNamespaces
        };

        var context = parser.cell();
        walker.Walk(listener, context);

        return new CellSplitResult(listener.CellParts, listener.GloballyOpenedNamespaces);
    }

    public static CellSplitResult SplitCell(string cellContents, INamespaceList? openNamespaces = null)
    {
        // Make a copy so that we don't mutate the original.
        openNamespaces = openNamespaces == null
                         ? new NamespaceList()
                         : new NamespaceList(openNamespaces);

        // Our strategy will be to preprocess out any magic commands, since
        // their inputs may have syntax that isn't valid in any Q# context.
        // While we could handle that in lexing, it's easiest to just split
        // them out.

        // Once we do, we can then split each chunk of declarations and
        // concatenate the cell parts that we get from each.

        var parts = new List<CellPart>();
        var currentChunk = "";
        void HandleChunk()
        {
            var chunkResults = SplitOneChunk(currentChunk, openNamespaces);
            openNamespaces = chunkResults.GloballyOpenNamespaces;
            parts.AddRange(chunkResults.Parts);
            currentChunk = "";
        }

        var lines = cellContents.Split(new[] { "\n", "\r", "\r\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            // Is it a magic command?
            if (line.Trim().StartsWith("%"))
            {
                // Handle whatever chunk we accumulated so far...
                HandleChunk();

                var lineParts = line.Trim().Split(" ", 2);
                var cmd = lineParts[0];
                var input = lineParts.Length == 2 ? lineParts[1] : "";
                parts.Add(new MagicCommandInvocation(cmd, input));
            }
            else
            {
                currentChunk += line + Environment.NewLine;
            }
        }

        // Did we have a chunk left over?
        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            HandleChunk();
        }

        return new CellSplitResult(parts, openNamespaces);
    }
}
