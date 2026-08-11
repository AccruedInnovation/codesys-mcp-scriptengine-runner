# -*- coding: utf-8 -*-
from __future__ import print_function

import sys

print("hello from the CODESYS ScriptEngine")
print("sys.argv: %r" % (sys.argv,))
print("mcp_arguments: %r" % (mcp_arguments,))
print("mcp_input_json: %r" % (mcp_input_json,))
print("primary project available: %s" % (projects.primary is not None,))

# The MCP tool reads this global after the script completes. For a structured
# result, set it to JSON text with json.dumps(...).
mcp_result = "hello_mcp.py completed"
