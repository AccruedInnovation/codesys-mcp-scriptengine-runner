# -*- coding: utf-8 -*-
from __future__ import print_function
import json
import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

from codesys_mcp_script_runner_common import invoke, wait_for_manifest_update

invoke("RegisterScriptRunner")
wait_for_manifest_update()

manifest_root = os.path.join(
    os.environ.get("LOCALAPPDATA", ""),
    "CODESYS",
    "Development System MCP Server")
found = []
if os.path.isdir(manifest_root):
    for current_root, dirs, files in os.walk(manifest_root):
        for filename in files:
            if filename.lower() != "manifest.json":
                continue
            path = os.path.join(current_root, filename)
            try:
                handle = open(path, "rb")
                try:
                    content = handle.read()
                finally:
                    handle.close()
                if b'run_codesys_script' in content:
                    found.append(path)
            except Exception:
                pass

if found:
    print("Manifest entry found:")
    for path in found:
        print("  " + path)
else:
    print("No generated manifest currently contains 'run_codesys_script'.")
    print("Check Tools > Options > MCP Server and allow the tool if listed.")
