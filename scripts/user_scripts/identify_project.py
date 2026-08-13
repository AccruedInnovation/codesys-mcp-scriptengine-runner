from __future__ import print_function
from scriptengine import *
import json
import os

def project_identity():
    project = projects.primary
    if project is None:
        return {}
    result = {}
    for name in ("name", "path", "guid", "active_application"):
        try:
            value = getattr(project, name)
            if name == "active_application":
                value = getattr(value, "name", str(value))
            result[name] = str(value)
        except Exception:
            pass
    try:
        result["name"] = str(project.get_name())
    except Exception:
        pass
    return result

result = project_identity()

mcp_result = json.dumps({"result": result})