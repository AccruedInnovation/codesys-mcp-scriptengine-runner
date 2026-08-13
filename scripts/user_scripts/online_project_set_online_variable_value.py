from scriptengine import *
import json

def project_start():
    ok = False
    error = "None"

    proj = projects.primary
    if proj is None:
        error = "No project is open."
        return ok, error

    # Log into the PLC 
    app = proj.active_application
    if app is None:
        error = "No application."
        return ok, error
    
    onlineapp = online.create_online_application(app)
    if onlineapp is None:
        error = "No online application."
        return ok, error
    
    onlineapp.set_prepared_value(mcp_arguments[0], mcp_arguments[1])
    onlineapp.write_prepared_values()
        
    ok = True
    return ok, error
    
ok, error = project_start()
mcp_result = json.dumps({"ok": ok, "error": error})