from scriptengine import *
import json
import sys

system.log_prompt_details = True
system.script_prompt_handling = ScriptPromptHandling.LogPrompts

system.prompt_answers["QueryProgramChanged"] = PromptResult.Yes
system.prompt_answers["CleanActiveApplication_Query"] = PromptResult.Yes
system.prompt_answers["QueryCreateApplication"] = PromptResult.Yes

def go_online():
    ok = False
    error = "None"

    proj = projects.primary
    if proj is None:
        error = "No project is open."
        return ok, error

    # Log into the PLC 
    app = proj.active_application
    app.clean()
    onlineapp = online.create_online_application(app)
    login_error = None
    
    try:
        onlineapp.login(OnlineChangeOption.Never, True)
    except:
        login_error = sys.exc_info()

    if login_error is not None:
        error = str(login_error[1])
        return ok, error
        
    ok = True
    return ok, error
    
ok, error = go_online()
mcp_result = json.dumps({"ok": ok, "error": error})