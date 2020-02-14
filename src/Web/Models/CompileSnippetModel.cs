namespace Microsoft.Quantum.IQSharp.Web.Models
{
    /// <summary>
    /// Model consumed by the <see cref="SnippetsController"/>.
    /// </summary>
    public class CompileSnippetModel
    {
        /// <summary>
        /// Q# source code to compile.
        /// </summary>
        public string Code { get; set; }
    }
}
