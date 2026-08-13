from __future__ import print_function
from scriptengine import *
import json
import os

class Reporter(ImportReporter):
    def __init__(self):
        self.errors = []
        self.warnings = []

    def error(self, message):
        self.errors.append(str(message))
        print("IMPORT ERROR: {}".format(message))

    def warning(self, message):
        self.warnings.append(str(message))
        print("IMPORT WARNING: {}".format(message))

    def resolve_conflict(self, obj):
        return ConflictResolve.Replace

    def added(self, obj):
        pass

    def replaced(self, obj):
        pass

    def skipped(self, objectname):
        self.errors.append("Importer skipped {}".format(objectname))

    @property
    def aborting(self):
        return bool(self.errors)

def import_with_reporter(project, xml_path):
    """Import via the active project API; never saves and always reacquires project."""
    ok = False
    error = "None"
    fail = False
    reporter = Reporter()
    project.import_xml(dataOrPath=xml_path, reporter=reporter, import_folder_structure=True)
    if reporter.errors:
        error = "PLCopenXML import reported errors: {}".format("; ".join(reporter.errors))
        fail = True
    return fail, reporter, error

project = projects.primary
xml_path = os.path.abspath(mcp_arguments[0])

if project is None:
    error = "No primary project is open"
    
else:
    fail, reporter, error = import_with_reporter(project, xml_path)
    
    if not fail:
        ok = True
        
mcp_result = json.dumps({"ok": ok, "error": error})