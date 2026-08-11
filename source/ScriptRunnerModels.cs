using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AccruedInnovation.CodesysMcp.ScriptRunner
{
    public sealed class RunCodesysScriptParameters
    {
        public RunCodesysScriptParameters()
        {
            Arguments = Array.Empty<string>();
            Trace = false;
            MaxOutputCharacters = 20000;
        }

        [Description(
            "Path to a Python file below the configured script root. " +
            "Relative paths are recommended; absolute paths are accepted only when they remain below that root.")]
        public string ScriptPath { get; set; } = null!;

        [Description(
            "Optional string arguments. The script path is supplied as sys.argv[0], followed by these values.")]
        public string[] Arguments { get; set; }

        [Description(
            "Optional JSON text exposed to the script as the global variable mcp_input_json.")]
        public string? InputJson { get; set; }

        [Description(
            "Enable CODESYS ScriptEngine statement tracing for this execution.")]
        public bool Trace { get; set; }

        [Description(
            "Maximum number of captured output characters returned to the MCP client. " +
            "Values are clamped to 1000 through 200000.")]
        public int MaxOutputCharacters { get; set; }
    }

    internal sealed class CapturedScriptMessage
    {
        public string Severity { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    internal sealed class ScriptRunResponse
    {
        public bool Ok { get; set; }
        public string Tool { get; set; } = "run_codesys_script";
        public string ScriptPath { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
        public int ExitCode { get; set; }
        public string? ExitArgument { get; set; }
        public bool McpResultSet { get; set; }
        public string? Result { get; set; }
        public string? ReturnValue { get; set; }
        public string? Error { get; set; }
        public List<CapturedScriptMessage> Messages { get; set; } =
            new List<CapturedScriptMessage>();
        public bool MessagesTruncated { get; set; }
    }
}
