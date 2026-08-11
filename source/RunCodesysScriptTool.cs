using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using _3S.CoDeSys.Core.Messages;
using _3S.CoDeSys.ScriptEngine;
using CODESYS.DevelopmentSystemMCPServer.Results;
using CODESYS.DevelopmentSystemMCPServer.Tools;

namespace AccruedInnovation.CodesysMcp.ScriptRunner
{
    internal sealed class ScriptRunnerAnnotations : IMcpToolAnnotations
    {
        public bool ReadOnlyHint => false;
        public bool DestructiveHint => true;
        public bool IdempotentHint => false;
        public bool OpenWorldHint => true;
    }

    internal sealed class RunCodesysScriptTool : IMcpTool
    {
        internal static readonly Guid ToolId =
            new Guid("DAA19B6E-9996-4C91-AB8A-330416085433");

        private const long MaximumScriptBytes = 4L * 1024L * 1024L;

        private readonly IToolResultFactory _results;
        private readonly IScriptEngine2 _scriptEngine;
        private readonly string _scriptsRoot;
        private readonly string _scriptsRootWithSeparator;
        private readonly IMcpToolAnnotations _annotations =
            new ScriptRunnerAnnotations();
        private int _executionActive;

        public RunCodesysScriptTool(
            IToolResultFactory results,
            IScriptEngine2 scriptEngine,
            string scriptsRoot)
        {
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _scriptEngine = scriptEngine ??
                throw new ArgumentNullException(nameof(scriptEngine));
            _scriptsRoot = Path.GetFullPath(scriptsRoot ??
                throw new ArgumentNullException(nameof(scriptsRoot)));
            _scriptsRootWithSeparator = EnsureTrailingSeparator(_scriptsRoot);
        }

        public Guid Id => ToolId;
        public string Name => "run_codesys_script";
        public string Title => "Run CODESYS ScriptEngine script";
        public string Description =>
            "Executes a trusted IronPython .py file inside the current CODESYS IDE " +
            "with the normal CODESYS scripting APIs. The call blocks until the script " +
            "finishes. Scripts can change projects, access files, start processes, or " +
            "use the network. Allowed script root: " + _scriptsRoot;
        public IMcpToolAnnotations Annotations => _annotations;
        public Type ParametersType => typeof(RunCodesysScriptParameters);

        public IToolResult Execute(
            object parameters,
            IToolExecutionContext context)
        {
            _ = context;
            if (!(parameters is RunCodesysScriptParameters input))
            {
                string actual = parameters == null
                    ? "null"
                    : parameters.GetType().FullName;
                return Error("Expected RunCodesysScriptParameters, but CODESYS supplied " +
                    actual + ".");
            }

            if (Interlocked.CompareExchange(ref _executionActive, 1, 0) != 0)
            {
                return Error(
                    "Another run_codesys_script call is already active. " +
                    "Wait for it to finish before starting another script.");
            }

            try
            {
                return ExecuteCore(input);
            }
            finally
            {
                Volatile.Write(ref _executionActive, 0);
            }
        }

        private IToolResult ExecuteCore(RunCodesysScriptParameters input)
        {
            string scriptPath;
            int outputLimit = Math.Max(
                1000,
                Math.Min(200000, input.MaxOutputCharacters));

            try
            {
                scriptPath = ResolveScriptPath(input.ScriptPath);
                long scriptBytes = new FileInfo(scriptPath).Length;
                if (scriptBytes > MaximumScriptBytes)
                {
                    throw new InvalidOperationException(
                        "The script is larger than the " +
                        MaximumScriptBytes.ToString(CultureInfo.InvariantCulture) +
                        " byte limit.");
                }
            }
            catch (Exception exception)
            {
                return Error(Serialize(new ScriptRunResponse
                {
                    Ok = false,
                    ScriptPath = input.ScriptPath ?? string.Empty,
                    Error = FormatExceptionChain(exception, outputLimit)
                }));
            }

            var collector = new MessageCollector(outputLimit);
            var response = new ScriptRunResponse
            {
                ScriptPath = scriptPath
            };
            var stopwatch = Stopwatch.StartNew();
            IScriptExecutor2? executor = null;
            ScriptExecutionEventArgs? executedArgs = null;

            try
            {
                executor = _scriptEngine.CreateScriptExecutor(
                    new Dictionary<string, object>());
                executor.ImportFilterActive = false;
                executor.ImplicitImport = true;
                executor.AddScriptDirectoryToPath = true;
                executor.TraceActive = input.Trace;

                if (executor is IScriptExecutor4 executor4)
                {
                    Action<IMessage>? defaultHandler =
                        executor4.DefaultMessageHandler;
                    executor4.MessageHandler = message =>
                    {
                        collector.Add(message);
                        if (defaultHandler != null)
                        {
                            defaultHandler(message);
                        }
                    };
                }

                executor.Executed += (_, eventArgs) =>
                {
                    executedArgs = eventArgs;
                };

                executor.LoadDrivers();
                IScriptScope scope = executor.ScriptScope;
                scope.SetVariable("mcp_script_path", scriptPath);
                scope.SetVariable("mcp_script_root", _scriptsRoot);
                scope.SetVariable(
                    "mcp_arguments",
                    input.Arguments ?? Array.Empty<string>());
                scope.SetVariable("mcp_input_json", input.InputJson);
                if (scope.ContainsVariable("mcp_result"))
                {
                    scope.RemoveVariable("mcp_result");
                }

                if (executor is IScriptExecutor3 executor3)
                {
                    executor3.ScriptArguments.Clear();
                    executor3.ScriptArguments.Add(scriptPath);
                    foreach (string argument in input.Arguments ??
                        Array.Empty<string>())
                    {
                        executor3.ScriptArguments.Add(argument ?? string.Empty);
                    }
                }

                EventHandler<ScriptNotificationEventArgs> notificationHandler =
                    (_, notification) => collector.Add(notification);
                IScriptSource source = executor.CompileFile(
                    scriptPath,
                    notificationHandler);
                executor.Execute(source, notificationHandler);

                object resultValue;
                if (scope.TryGetVariable("mcp_result", out resultValue))
                {
                    response.McpResultSet = true;
                    response.Result = FormatValue(resultValue, outputLimit);
                }

                if (executedArgs is ScriptExecutionEventArgs3 eventArgs3)
                {
                    response.ReturnValue = FormatValue(
                        eventArgs3.ReturnValue,
                        outputLimit);
                }

                if (executedArgs != null)
                {
                    response.ExitCode = executedArgs.ExitCode;
                    response.ExitArgument = FormatValue(
                        executedArgs.ExitArg,
                        outputLimit);
                    if (executedArgs.Exception != null)
                    {
                        response.Error = FormatExceptionChain(
                            executedArgs.Exception,
                            outputLimit);
                    }
                }

                response.Ok = response.Error == null && response.ExitCode == 0;
                if (!response.Ok && response.Error == null)
                {
                    response.Error =
                        "The script ended with exit code " +
                        response.ExitCode.ToString(CultureInfo.InvariantCulture) + ".";
                }
            }
            catch (Exception exception)
            {
                response.Ok = false;
                response.Error = FormatExceptionChain(exception, outputLimit);
                if (executedArgs != null)
                {
                    response.ExitCode = executedArgs.ExitCode;
                    response.ExitArgument = FormatValue(
                        executedArgs.ExitArg,
                        outputLimit);
                }
            }
            finally
            {
                stopwatch.Stop();
                response.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                response.Messages = collector.Messages;
                response.MessagesTruncated = collector.Truncated;
                (executor as IDisposable)?.Dispose();
            }

            string json = Serialize(response);
            return response.Ok ? Success(json) : Error(json);
        }

        private string ResolveScriptPath(string requestedPath)
        {
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                throw new ArgumentException("script_path is required.");
            }

            string expanded = Environment.ExpandEnvironmentVariables(
                requestedPath.Trim());
            string candidate = Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(_scriptsRoot, expanded);
            string fullPath = Path.GetFullPath(candidate);

            bool isRoot = string.Equals(
                fullPath,
                _scriptsRoot,
                StringComparison.OrdinalIgnoreCase);
            bool isBelowRoot = fullPath.StartsWith(
                _scriptsRootWithSeparator,
                StringComparison.OrdinalIgnoreCase);
            if (isRoot || !isBelowRoot)
            {
                throw new UnauthorizedAccessException(
                    "The script must be below the configured root: " +
                    _scriptsRoot);
            }

            if (!string.Equals(
                Path.GetExtension(fullPath),
                ".py",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Only files with the .py extension may be executed.");
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "The requested script does not exist.",
                    fullPath);
            }

            return fullPath;
        }

        private IToolResult Success(string text)
        {
            IToolContent content = _results.CreateTextContent(text);
            return _results.CreateResult(new[] { content }, false);
        }

        private IToolResult Error(string text)
        {
            IToolContent content = _results.CreateTextContent(text);
            return _results.CreateResult(new[] { content }, true);
        }

        private static string Serialize(ScriptRunResponse response)
        {
            var payload = new Dictionary<string, object>
            {
                ["ok"] = response.Ok,
                ["tool"] = response.Tool,
                ["script_path"] = response.ScriptPath,
                ["elapsed_ms"] = response.ElapsedMilliseconds,
                ["exit_code"] = response.ExitCode,
                ["mcp_result_set"] = response.McpResultSet,
                ["messages_truncated"] = response.MessagesTruncated
            };

            if (response.ExitArgument != null)
            {
                payload["exit_argument"] = response.ExitArgument;
            }
            if (response.Result != null)
            {
                payload["mcp_result"] = response.Result;
            }
            if (response.ReturnValue != null)
            {
                payload["return_value"] = response.ReturnValue;
            }
            if (response.Error != null)
            {
                payload["error"] = response.Error;
            }

            var messages = new List<Dictionary<string, object>>();
            foreach (CapturedScriptMessage message in response.Messages)
            {
                messages.Add(new Dictionary<string, object>
                {
                    ["severity"] = message.Severity,
                    ["text"] = message.Text
                });
            }
            payload["messages"] = messages;

            var serializer = new JavaScriptSerializer
            {
                MaxJsonLength = 1024 * 1024,
                RecursionLimit = 20
            };
            return serializer.Serialize(payload);
        }

        private static string? FormatValue(object? value, int limit)
        {
            if (value == null)
            {
                return null;
            }

            string text;
            if (value is string stringValue)
            {
                text = stringValue;
            }
            else
            {
                try
                {
                    text = Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture) ?? string.Empty;
                }
                catch
                {
                    text = value.ToString() ?? string.Empty;
                }
            }

            return Truncate(text, limit);
        }

        private static string FormatExceptionChain(Exception exception, int limit)
        {
            var parts = new List<string>();
            Exception? current = exception;
            int depth = 0;
            while (current != null && depth < 12)
            {
                string part = current.GetType().FullName + ": " + current.Message;
                if (current is SyntaxErrorException syntax)
                {
                    part += Environment.NewLine + string.Format(
                        CultureInfo.InvariantCulture,
                        "location: {0}({1},{2})",
                        string.IsNullOrEmpty(syntax.File) ? "<script>" : syntax.File,
                        syntax.Line,
                        syntax.Column);
                }

                if (current.Data != null &&
                    current.Data.Contains("_3S.CoDeSys.ScriptEngine.Formatted"))
                {
                    object formatted =
                        current.Data["_3S.CoDeSys.ScriptEngine.Formatted"];
                    if (formatted != null)
                    {
                        part += Environment.NewLine + formatted;
                    }
                }

                parts.Add(part);
                current = current.InnerException;
                depth++;
            }

            return Truncate(
                string.Join(Environment.NewLine + "caused by: ", parts),
                limit);
        }

        private static string Truncate(string text, int limit)
        {
            if (text.Length <= limit)
            {
                return text;
            }

            const string suffix = "\n<output truncated>";
            int keep = Math.Max(0, limit - suffix.Length);
            return text.Substring(0, keep) + suffix;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ||
                path.EndsWith(
                    Path.AltDirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private sealed class MessageCollector
        {
            private readonly int _limit;
            private int _used;

            public MessageCollector(int limit)
            {
                _limit = limit;
            }

            public List<CapturedScriptMessage> Messages { get; } =
                new List<CapturedScriptMessage>();

            public bool Truncated { get; private set; }

            public void Add(IMessage message)
            {
                if (message == null)
                {
                    return;
                }

                Add(message.Severity.ToString(), message.Text ?? string.Empty);
            }

            public void Add(ScriptNotificationEventArgs notification)
            {
                if (notification == null)
                {
                    return;
                }

                string location = string.IsNullOrEmpty(notification.Path)
                    ? "<script>"
                    : notification.Path;
                string text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}({1},{2}): {3}",
                    location,
                    notification.StartLine,
                    notification.StartColumn,
                    notification.Message ?? string.Empty);
                Add(notification.Severity.ToString(), text);
            }

            private void Add(string severity, string text)
            {
                if (Truncated)
                {
                    return;
                }

                int remaining = _limit - _used;
                if (remaining <= 0)
                {
                    Truncated = true;
                    return;
                }

                if (text.Length > remaining)
                {
                    text = text.Substring(0, remaining);
                    Truncated = true;
                }

                Messages.Add(new CapturedScriptMessage
                {
                    Severity = severity,
                    Text = text
                });
                _used += text.Length;
            }
        }
    }
}
