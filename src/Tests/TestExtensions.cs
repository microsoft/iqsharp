// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Text.RegularExpressions;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp;
internal static class TestExtensions
{
    internal record ObjectAssert(object? Object);

    internal static ObjectAssert Object(this Assert assert, object? obj) =>
        new ObjectAssert(obj);

    internal static T IsInstanceOfType<T>(this ObjectAssert assert, string message)
    {
        Assert.IsNotNull(assert.Object);
        Assert.IsInstanceOfType(assert.Object, typeof(T), message);
        return (T)assert.Object;
    }

    internal record class WorkspaceAssert(IWorkspace Workspace);

    internal static WorkspaceAssert Workspace(this Assert assert, IWorkspace workspace) =>
        new WorkspaceAssert(workspace);
    internal static WorkspaceAssert DoesNotHaveErrors(this WorkspaceAssert assert)
    {
        var ws = assert.Workspace;
        if (ws.HasErrors)
        {
            var msg = "Expected workspace initialization to succeed, but got errors:\n";
            Assert.Fail(msg + string.Join("\n", ws.ErrorMessages.OrEmpty().Select(err => $"- {err}")));
        }
        return assert;
    }

    internal record InputAssert(string Code, int? CursorPos, IQSharpEngine Engine) : UsingEngineAssert(Engine);        internal record UsingEngineAssert(IQSharpEngine Engine);

    internal static async Task<T> WithMockAzure<T>(this Task<T> engine)
    where T: UsingEngineAssert
    {
        // Start by connecting using the mock connection string.
        await engine
            .Input("%azure.connect subscription=TEST_SUBSCRIPTION_ID resourceGroup=TEST_RESOURCE_GROUP_NAME workspace=NameWithMockProviders storage=TEST_CONNECTION_STRING location=TEST_LOCATION")
            .ExecutesSuccessfully();
        var client = await (await engine).Engine.GetEngineService<IAzureClient>();
        if (client is AzureClient azureClient && azureClient.ActiveWorkspace is MockAzureWorkspace workspace)
        {
            workspace.AddProviders("ionq", "quantinuum", "honeywell");
        }
        else
        {
            throw new Exception("Expected a mock workspace to be set as the active Azure workspace.");
        }
        return await engine;
    }

    internal static void AssertIsOk(this ExecutionResult response) =>
        Assert.AreEqual(ExecuteStatus.Ok, response.Status, $"Response was not marked as Ok.\n\tActual status: {response.Status}\n\tResponse output: {response.Output}");


    internal async static Task<InputAssert> Input<T>(this Task<T> assert, string code, int? cursorPos = null)
    where T: UsingEngineAssert =>
        (await assert).Input(code, cursorPos);

    internal static InputAssert Input(this UsingEngineAssert assert, string code, int? cursorPos = null) =>
        new InputAssert(code, cursorPos, assert.Engine);

    internal static UsingEngineAssert UsingEngine(this Assert _, IQSharpEngine engine) =>
        new UsingEngineAssert(engine);

    internal static async Task<UsingEngineAssert> UsingEngine(this Assert assert) =>
        assert.UsingEngine(await IQSharpEngineTests.Init("Workspace"));

    internal static async Task<UsingEngineAssert> UsingEngine(this Assert assert, Func<IServiceProvider, Task> configure) =>
        assert.UsingEngine(await IQSharpEngineTests.Init("Workspace", configure: configure));

    internal static async Task<T> ExecutesWithStatus<T>(this Task<T> input, ExecuteStatus expected)
    where T: InputAssert =>
        await (await input).ExecutesWithStatus(expected);

    internal static async Task<T> ExecutesWithStatus<T>(this T input, ExecuteStatus expected)
    where T: InputAssert
    {
        var channel = new MockChannel();
        var result = await input.Engine.Execute(input.Code, channel);
        try
        {
            Assert.AreEqual(result.Status, expected);
        }
        catch (AssertFailedException ex)
        {
            var msg = $"Cell did not execute with status {expected}.\nOutput: \n{result.Output}\nStatus: {result.Status}\nException:\n{ex}\nCode:\n{input.Code}\n\nErrors:\n{string.Join("\n", channel.errors)}";
            throw new AssertFailedException(msg, ex);
        }

        return input;
    }


    internal static async Task<T> ExecutesSuccessfully<T>(this Task<T> input)
    where T: InputAssert =>
        await (await input).ExecutesSuccessfully();

    internal static async Task<T> ExecutesSuccessfully<T>(this T input)
    where T: InputAssert
    {
        var channel = new MockChannel();
        var result = await input.Engine.Execute(input.Code, channel);
        try
        {
            Assert.AreEqual(result.Status, ExecuteStatus.Ok);
        }
        catch (AssertFailedException ex)
        {
            var msg = $"Cell did not execute successfully.\nOutput: \n{result.Output}\nStatus: {result.Status}\nException:\n{ex}\nCode:\n{input.Code}\n\nErrors:\n{string.Join("\n", channel.errors)}";
            throw new AssertFailedException(msg, ex);
        }

        return input;
    }

    internal static async Task<T> ExecutesWithError<T>(this Task<T> input, Func<List<string>, bool>? where = null)
    where T: InputAssert =>
        await (await input).ExecutesWithError(where);

    internal static async Task<T> ExecutesWithError<T>(this Task<T> input, string containing)
    where T: InputAssert =>
        await input.ExecutesWithError(where: errors =>
            string.Join(Environment.NewLine, errors)
                    .NormalizeLineEndings()
                    .Contains(containing.NormalizeLineEndings()));

    internal static async Task<T> ExecutesWithError<T>(this T input, Func<List<string>, bool>? onErrors = null)
    where T: InputAssert
    {
        var channel = new MockChannel();
        var result = await input.Engine.Execute(input.Code, channel);
        try
        {
            Assert.AreEqual(result.Status, ExecuteStatus.Error);
        }
        catch (AssertFailedException ex)
        {
            var msg = $"Cell did not result in an error.\nOutput: \n{result.Output}\nStatus: {result.Status}\nException:\n{ex}\nCode:\n{input.Code}\n\nErrors:\n{string.Join("\n", channel.errors)}";
            throw new AssertFailedException(msg, ex);
        }

        if (onErrors != null)
        {
            try
            {
                Assert.IsTrue(onErrors(channel.errors));
            }
            catch (AssertFailedException ex)
            {
                var msg = $"Cell errors did not meet conditions specified by `onErrors`.\nOutput: \n{result.Output}\nStatus: {result.Status}\nException:\n{ex}\nCode:\n{input.Code}\n\nErrors:\n{string.Join("\n", channel.errors)}";
                throw new AssertFailedException(msg, ex);
            }
        }

        return input;
    }

    internal static async Task<T> CompletesTo<T>(this Task<T> input, params string[] expectedCompletions)
    where T: InputAssert =>
        await (await input).CompletesTo(expectedCompletions);

    internal static async Task<T> CompletesTo<T>(this T input, params string[] expectedCompletions)
    where T: InputAssert
    {
        var actualCompletions = await input.Engine.Complete(input.Code, input.CursorPos ?? 0);
        Assert.IsNotNull(actualCompletions, "Engine returned null for completions.");
        Assert.IsNotNull(actualCompletions.Value.Matches, "Engine returned completions with null matches.");

        var actualMatches = actualCompletions.Value.Matches.OrderBy(match => match).ToList();
        var expectedMatches = expectedCompletions.OrderBy(match => match).ToList();

        try
        {
            Assert.AreEqual(actualMatches.Count, expectedMatches.Count);
            foreach (var (actual, expected) in actualMatches.Zip(expectedMatches))
            {
                Assert.AreEqual(actual, expected);
            }
        }
        catch (AssertFailedException ex)
        {
            var message = $"Expected completions [{string.Join(", ", expectedMatches)}], but engine returned completions [{string.Join(", ", actualMatches)}]";
            throw new AssertFailedException(message, ex);
        }

        return input;
    }

    internal record AssemblyAssert(AssemblyInfo AssemblyInfo);

    internal static AssemblyAssert Assembly(this Assert assert, AssemblyInfo info) =>
        new AssemblyAssert(info);

    internal static T HasResource<T>(this T assert, string resourceName)
    where T: AssemblyAssert
    {
        Assert.IsTrue(
            assert.AssemblyInfo.Assembly.GetManifestResourceNames().Contains(resourceName),
            $"Assembly {assert.AssemblyInfo.Assembly.GetName()} did not contain a resource with name {resourceName}."
        );
        return assert;
    }

    internal static T HasOperation<T>(this T assert, string namespaceName, string name)
    where T: AssemblyAssert
    {
        Assert.IsTrue(
            assert.AssemblyInfo.Operations.Any(opInfo =>
                opInfo.Header.QualifiedName.Namespace == namespaceName &&
                opInfo.Header.QualifiedName.Name == name
            ),
            $"Assembly {assert.AssemblyInfo.Assembly.GetName()} did not contain an operation with name {namespaceName}.{name}."
        );
        return assert;
    }

    internal static string NormalizeLineEndings(this string s) =>
        Regex.Replace(s, @"\r\n|\n\r|\n|\r", "\r\n");

    

    internal record EnumerableAssert<T>(IEnumerable<T> Enumerable);

    internal static EnumerableAssert<T> Enumerable<T>(this Assert assert, IEnumerable<T> enumerable) =>
        new EnumerableAssert<T>(enumerable);

    internal static EnumerableAssert<T> HasCount<T>(this EnumerableAssert<T> enumerableAssert, int count)
    {
        // Collect in a list so that we can report on failure.
        var elements = enumerableAssert.Enumerable.ToList();
        Assert.AreEqual(
            count,
            elements.Count,
            $"Expected {count} elements, but got {elements.Count}. Enumerable yielded values:\n{string.Join("\n", elements.Select(e => $"    - {e?.ToString() ?? "<null>"}"))}"
        );
        return enumerableAssert;
    }

    internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) =>
        source.Where(e => e is not null).Select(e => e!);

    internal static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) =>
        source ?? System.Linq.Enumerable.Empty<T>();

}
