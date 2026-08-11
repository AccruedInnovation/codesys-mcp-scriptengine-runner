using System;
using System.IO;
using CODESYS.DevelopmentSystemMCPServer.Registration;
using CODESYS.DevelopmentSystemMCPServer.Results;
using _3S.CoDeSys.ScriptEngine;

namespace AccruedInnovation.CodesysMcp.ScriptRunner
{
    public static class RuntimeRegistration
    {
        private static readonly object Sync = new object();
        private static McpDependencyBag? _dependencies;

        private static readonly Guid NamespaceId =
            new Guid("BD608414-81AB-4981-AF74-6D8C5D6807D4");

        private static McpDependencyBag Dependencies
        {
            get
            {
                lock (Sync)
                {
                    return _dependencies ?? (_dependencies = new McpDependencyBag());
                }
            }
        }

        public static string Probe()
        {
            McpDependencyBag dependencies = Dependencies;
            IMcpRegistry registry = dependencies.McpRegistryProvider.Value;
            IMcpNamespaceFactory namespaceFactory =
                dependencies.NamespaceFactoryProvider.Value;
            IToolResultFactory resultFactory =
                dependencies.ToolResultFactoryProvider.Value;
            IScriptEngine2 scriptEngine =
                dependencies.ScriptEngineProvider.Value;

            return string.Join(
                Environment.NewLine,
                "Script runner dependencies resolved.",
                "MCP registry: " + registry.GetType().AssemblyQualifiedName,
                "Namespace factory: " + namespaceFactory.GetType().AssemblyQualifiedName,
                "Result factory: " + resultFactory.GetType().AssemblyQualifiedName,
                "Script engine: " + scriptEngine.GetType().AssemblyQualifiedName,
                "run_codesys_script registered: " +
                    registry.IsToolRegistered(RunCodesysScriptTool.ToolId),
                "Default script root: " + GetDefaultScriptRoot());
        }

        public static string RegisterScriptRunner()
        {
            return RegisterScriptRunnerAt(GetDefaultScriptRoot());
        }

        public static string RegisterScriptRunnerAt(string scriptsRoot)
        {
            if (string.IsNullOrWhiteSpace(scriptsRoot))
            {
                throw new ArgumentException(
                    "A non-empty script root is required.",
                    nameof(scriptsRoot));
            }

            string normalizedRoot = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(scriptsRoot));
            Directory.CreateDirectory(normalizedRoot);

            McpDependencyBag dependencies = Dependencies;
            IMcpRegistry registry = dependencies.McpRegistryProvider.Value;
            IMcpNamespace owner = dependencies.NamespaceFactoryProvider.Value.Create(
                NamespaceId,
                "Accrued Innovation");

            var tool = new RunCodesysScriptTool(
                dependencies.ToolResultFactoryProvider.Value,
                dependencies.ScriptEngineProvider.Value,
                normalizedRoot);

            if (registry.IsToolRegistered(tool.Id))
            {
                registry.UnregisterTool(tool.Id);
            }

            registry.RegisterTool(owner, tool);
            return string.Join(
                Environment.NewLine,
                "Registered run_codesys_script (" + tool.Id + ").",
                "Script root: " + normalizedRoot,
                "Only .py files below this root may be selected by the MCP call.");
        }

        public static string UnregisterScriptRunner()
        {
            bool removed = Dependencies.McpRegistryProvider.Value
                .UnregisterTool(RunCodesysScriptTool.ToolId);
            return removed
                ? "Unregistered run_codesys_script."
                : "run_codesys_script was not registered.";
        }

        public static string GetDefaultScriptRoot()
        {
            string? assemblyDirectory = Path.GetDirectoryName(
                typeof(RuntimeRegistration).Assembly.Location);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                assemblyDirectory = Environment.CurrentDirectory;
            }

            string configurationPath = Path.Combine(
                assemblyDirectory,
                "script-root.txt");
            if (File.Exists(configurationPath))
            {
                foreach (string rawLine in File.ReadAllLines(configurationPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string expanded = Environment.ExpandEnvironmentVariables(line);
                    string configured = Path.IsPathRooted(expanded)
                        ? expanded
                        : Path.Combine(assemblyDirectory, expanded);
                    return Path.GetFullPath(configured);
                }
            }

            return Path.Combine(assemblyDirectory, "user_scripts");
        }
    }
}
