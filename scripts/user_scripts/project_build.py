from scriptengine import *
import json

ok = False
error = "None"

def count_build_errors(project):
    POUGuid = Guid("6f9dac99-8de1-4efc-8465-68ac443b7d08")
    BUILD_CATEGORY_GUID = "97f48d64-a2a3-4856-b640-75c046e37ea9"

    try:
        messages = system.get_message_objects(
            category=BUILD_CATEGORY_GUID
        )
    except:
        return -1

    error_count = 0

    for message in messages:
        try:
            severity_text = str(message.severity)

            # Do not include informational build messages.
            if severity_text == "Information":
                continue

            # Count error messages.
            if severity_text == "Error":
                error_count = error_count + 1

        except:
            continue

    return error_count

project = projects.primary

if project is None:
    error = "No primary project is open"
    
else:
    project.active_application.build()

    build_count = count_build_errors(project)

    if build_count == -1:
        ok = False
        error = "Build message retreival failed"
    elif build_count > 0:
        ok = False
        error = "{} Build Errors".format(str(build_count))
        
mcp_result = json.dumps({"ok": ok, "error": error})