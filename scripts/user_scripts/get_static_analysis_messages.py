from scriptengine import *
import json

def get_sa_messages():
    """
    Returns:
        Number of messages written, or -1 if message retrieval fails.
    """
    project = projects.primary
    POUGuid = Guid("6f9dac99-8de1-4efc-8465-68ac443b7d08")
    STATIC_ANALYSIS_CATEGORY_GUID = "220493a1-f49b-4416-9a3f-a545db707cbe"

    log_lines = []
    error_count = 0
    
    try:
        messages = system.get_message_objects(
            category=STATIC_ANALYSIS_CATEGORY_GUID
        )
    except:
        return -1, 0

    for message in messages:
        try:
            if message.object.guid != POUGuid:
                parent_text = str(
                    message.object.parent.get_name()
                )
                object_text = str(message.object.get_name())
            else:
                parent_text = str(message.object.get_name())
                object_text = "-"
        except:
            parent_text = "-"
            object_text = "-"
                
        try:
            message_text = str(
                message.text.encode("utf-8")
            ).replace("\r", "").replace("\n", "")
        except:
            message_text = "-"

        try:
            position_text = str(
                message.position_text.encode("utf-8")
            )
        except:
            position_text = "-"
            
        try:
            rule_text = str(message.number)
        except:
            rule_text = "-"
            
        try:
            severity_text = str(message.severity)
            # Count error messages.
            if severity_text == "Error":
                error_count = error_count + 1
        except:
            severity_text = "-"

        log_lines.append(
            parent_text
            + " | "
            + object_text
            + " | "
            + message_text
            + " | "
            + position_text
            + " | "
            + rule_text
            + " | "
            + severity_text
            + "\n"
        )

    return error_count, log_lines

count, errors = get_sa_messages()
mcp_result = json.dumps({"count": count, "errors": errors})