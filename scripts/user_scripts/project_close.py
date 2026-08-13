from __future__ import print_function
from scriptengine import *
import json

ok = False
error = "None"

project = projects.primary

if project is None:
    error = "No primary project is open"
    
else:
    project.close()
    ok = True
        
mcp_result = json.dumps({"ok": ok, "error": error})