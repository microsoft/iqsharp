namespace Microsoft.Quantum.IQSharp.CellParser;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System.IO;

internal static class AntrlExtensions
{
    internal record SyntaxError<TSymbol>(
        TextWriter Output, IRecognizer Recognizer, TSymbol OffendingSymbol, int Line, int CharPositionInLine, string Msg, RecognitionException Exception
    );

    internal class ActionErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
    {
        internal readonly Action<SyntaxError<TSymbol>> Action;

        internal ActionErrorListener(Action<SyntaxError<TSymbol>> action)
        {
            Action = action;
        }

        void IAntlrErrorListener<TSymbol>.SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Action(new SyntaxError<TSymbol>(
                output, recognizer, offendingSymbol, line, charPositionInLine, msg, e
            ));
        }
    }

    public static void AddErrorListener<TSymbol, TInterpreter>(this Recognizer<TSymbol, TInterpreter> recognizer, Action<SyntaxError<TSymbol>> listener)
    where TInterpreter : Antlr4.Runtime.Atn.ATNSimulator
    {
        recognizer.AddErrorListener(new ActionErrorListener<TSymbol>(listener));
    }

    public static string ToDottedName(this IQSharpCellParser.QualifiedNameContext context) =>
        string.Join(".",
            context.Identifier().Select(node =>
                    context.Start.InputStream.GetText(new Interval(
                        node.Symbol.StartIndex,
                        node.Symbol.StopIndex
                    ))
                )
        );

}