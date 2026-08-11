# CODESYS MCP ScriptEngine Runner 1.0.0

This package registers a run-time MCP tool named `run_codesys_script` in a running CODESYS Development System. It uses a run-time registration route that does not install or patch a CODESYS plug-in.

## Compatibility

Built against:

- CODESYS Development System 3.5.22.20
- CODESYS Development System MCP Server 1.1.0.0
- `ComponentModel.dll` 3.5.22.20
- `ScriptEngine3.dll` 4.2.0.0
- .NET Framework 4.8, AnyCPU

The DLL does not include CODESYS assemblies. It uses the copies already loaded by the IDE.

## What the tool does

`run_codesys_script` executes an existing IronPython `.py` file through the CODESYS ScriptEngine inside the current IDE process. It:

- supplies normal CODESYS ScriptEngine globals such as `projects`;
- puts the script path and arguments in `sys.argv`;
- exposes `mcp_script_path`, `mcp_script_root`, `mcp_arguments`, and `mcp_input_json` as globals;
- reads an optional `mcp_result` global after the script finishes;
- returns ScriptEngine messages, compile notices, exit data, and errors as JSON text;
- forwards captured messages to the normal CODESYS message handler, so they remain visible in the IDE.

The tool runs one script at a time. The MCP call and the CODESYS main thread remain blocked until the script finishes.

## Install and register

1. Extract the package to a writable folder, for example:

   ```text
   D:\Projects\codesys-mcp\script-runner
   ```

2. Use the files in the `scripts` folder. Keep these items together:

   ```text
   AccruedInnovation.CodesysMcp.ScriptRunner.dll
   AccruedInnovation.CodesysMcp.ScriptRunner.pdb
   codesys_mcp_script_runner_common.py
   probe_script_runner.py
   register_script_runner.py
   unregister_script_runner.py
   script-root.txt
   user_scripts\
   ```

3. Edit `script-root.txt` when you want another approved script folder. The first non-empty, non-comment line sets the root. A relative path resolves from the DLL folder. The supplied setting is:

   ```text
   user_scripts
   ```

4. Start CODESYS and enable the MCP Server.

5. In CODESYS, run `scripts\probe_script_runner.py` with the ScriptEngine command for executing a script file.

   Before registration, this line is expected:

   ```text
   run_codesys_script registered: False
   ```

   The probe only checks service injection and prints the chosen script root. It does not expect a manifest entry before registration.

6. Run `scripts\register_script_runner.py` in the same way.

7. Open:

   ```text
   Tools > Options > MCP Server
   ```

   Find the `Accrued Innovation` origin and allow `Run CODESYS ScriptEngine script` if needed.

8. Refresh or reconnect the MCP client. The generated manifest should expose `run_codesys_script`.

Registration lasts for the current CODESYS process. Run `register_script_runner.py` again after restarting CODESYS.

## Smoke test

Call `run_codesys_script` with:

```json
{
  "script_path": "hello_mcp.py",
  "arguments": ["one", "two"],
  "input_json": "{\"job\":\"smoke-test\"}",
  "trace": false,
  "max_output_characters": 20000
}
```

Only `script_path` is required. The other fields have defaults.

A successful result has this form:

```json
{
  "ok": true,
  "tool": "run_codesys_script",
  "script_path": "D:\\Projects\\codesys-mcp\\script-runner\\scripts\\user_scripts\\hello_mcp.py",
  "elapsed_ms": 25,
  "exit_code": 0,
  "mcp_result_set": true,
  "mcp_result": "hello_mcp.py completed",
  "messages_truncated": false,
  "messages": [
    {
      "severity": "Information",
      "text": "hello from the CODESYS ScriptEngine"
    }
  ]
}
```

Exact message severity text and elapsed time depend on CODESYS.

## Script inputs and result

The script receives:

```python
# Normal command-line form
sys.argv[0]       # full script path
sys.argv[1:]      # values from arguments

# Added globals
mcp_script_path   # full script path
mcp_script_root   # configured approved root
mcp_arguments     # .NET string array
mcp_input_json    # optional JSON text or None
```

Set `mcp_result` to return a value:

```python
import json
mcp_result = json.dumps({"ok": True, "items": 3})
```

The tool converts `mcp_result` to text. Use `json.dumps` in the script when the caller needs structured JSON.

## Limits and safety

This is a path allowlist, not a security sandbox.

- The selected file must end in `.py`, exist below the configured root, and be no larger than 4 MiB.
- The path check rejects normal absolute-path and `..` escapes. A junction or symbolic link inside the root can still point elsewhere.
- Once started, the script has the normal rights of the CODESYS process. It can change projects, connect to controllers, read or write files, start processes, and use the network.
- The MCP tool is marked destructive, non-idempotent, and open-world.
- There is no safe hard timeout for a ScriptEngine call on the CODESYS main thread. A hung script can hang the IDE and the MCP request.
- Review scripts before placing them under the approved root. Do not expose this tool to an untrusted MCP client.

## Remove for the current session

Run:

```text
scripts\unregister_script_runner.py
```

Restarting CODESYS also removes all run-time registrations.

## Build from source

The `source` folder contains an SDK-style .NET Framework 4.8 project. Set `CodesysCommonDir` to the matching CODESYS `Common` directory and build:

```powershell
dotnet build `
  .\source\AccruedInnovation.CodesysMcp.ScriptRunner.csproj `
  -c Release `
  -p:CodesysCommonDir="C:\Program Files\CODESYS 3.5.22.20\CODESYS\Common"
```

The project references CODESYS assemblies with `Private=false`; it does not copy them into the output.
