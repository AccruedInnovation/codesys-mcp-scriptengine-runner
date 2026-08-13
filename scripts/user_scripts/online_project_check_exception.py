from __future__ import print_function
from scriptengine import *
import json

def check_exception():
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
        
    state = onlineapp.operation_state
    if state & OperatingState.exception:
        error = "Exception"
        print("FAILURE: The PLC threw an EXCEPTION. Last test message:")
        try:
            last_message = onlineapp.read_value("PRG_Test.LastTestMessage")
            print(last_message)
        except:
            print("Last test message unavailable: {}".format(
                _diagnostic_failure_text()))
        try:
            diagnostic_path = capture_exception_diagnostics(
                run_number, onlineapp, state)
            if diagnostic_path is not None:
                print("Exception diagnostics: {}".format(diagnostic_path))
        except:
            print("Exception diagnostic hook failed: {}".format(
                _diagnostic_failure_text()))
        return ok, error
        
    ok = True
    return ok, error
    
ok, error = check_exception()
mcp_result = json.dumps({"ok": ok, "error": error})