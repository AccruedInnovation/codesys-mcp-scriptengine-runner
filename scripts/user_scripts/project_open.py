from __future__ import print_function
from scriptengine import *
import json
import os

ok = False
error = "None"

path = os.path.abspath(mcp_arguments[0])

project = projects.open(path, primary = True)

if project is None:
    error = "Project failed to open"
    
else:
    ok = True

mcp_result = json.dumps({"ok": ok, "error": error})