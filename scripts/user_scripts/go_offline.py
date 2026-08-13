from scriptengine import *
import json

def go_offline():
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
        
    onlineapp.logout()
        
    ok = True
    return ok, error
    
ok, error = go_offline()
mcp_result = json.dumps({"ok": ok, "error": error})