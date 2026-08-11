from __future__ import print_function
from scriptengine import *
import io, os, re, sys
from datetime import datetime

############################################################################
################################# Variables
############################################################################

# Debug flags
debug = True
extendedDebug = False

############################################################################
################################# GUIDs (mirror CodesysGitExport.py exactly)
############################################################################

Project_Settings = Guid('8753fe6f-4a22-4320-8103-e553c4fc8e04')  # root node?

folderGUID = Guid('738bea1e-99bb-4f04-90bb-a7a567e74e3a')

GVLGuid = Guid('ffbfa93a-b94d-45fc-a329-229860183b1d')
GVLPersistentGUID = Guid('261bd6e6-249c-4232-bb6f-84c2fbeef430')

POUGuid = Guid("6f9dac99-8de1-4efc-8465-68ac443b7d08")
DUTGuid = Guid("2db5746d-d284-4425-9f7f-2663a34b0ebc")

methodwithoutGUID = Guid("f89f7675-27f1-46b3-8abb-b7da8e774ffd")
methodGUID = Guid("f8a58466-d7f6-439f-bbb8-d4600e41d099")
actionGUID = Guid('8ac092e5-3128-4e26-9e7e-11016c6684f2')

propertyGUID = Guid('5a3b8626-d3e9-4f37-98b5-66420063d91e')
propertyMethodGUID = Guid('792f2eb6-721e-4e64-ba20-bc98351056db')

intfGUID = Guid('6654496c-404d-479a-aad2-8551054e5f1e')

# Reverse mapping: prefix -> GUID (matching CodesysGitExport.py gittable_nodes)
PREFIX_TO_GUID = {
    'POU':  POUGuid,
    'DUT':  DUTGuid,
    'GVL':  GVLGuid,
    'GVLP': GVLPersistentGUID,
    'M':    methodwithoutGUID,   # methods may be either GUID; methodwithoutGUID for new creates
    'A':    actionGUID,
    'P':    propertyGUID,
    'PM':   propertyMethodGUID,
    'I':    intfGUID,
    'F':    folderGUID,
}

# Also build the reverse set of prefix strings for matching
ALL_PREFIXES = set(PREFIX_TO_GUID.keys())

############################################################################
################################# Timestamps and print helpers
############################################################################

start = datetime.now()

old_print = print


def timestamped_print(*args, **kwargs):
    old_print(datetime.now(), *args, **kwargs)


def debug_timestamped_print(*args, **kwargs):
    if debug or extendedDebug:
        old_print(datetime.now(), *args, **kwargs)


def extended_debug_timestamped_print(*args, **kwargs):
    if extendedDebug:
        old_print(datetime.now(), *args, **kwargs)


print = timestamped_print
debugPrint = debug_timestamped_print
eDebugPrint = extended_debug_timestamped_print

############################################################################
################################# Delete Logic
############################################################################

def delete_existing_root_objects(proj):
    """Remove all gittable objects from the project root.
    Only removes objects whose type GUID maps to a known PREFIX_TO_GUID key
    (POU, DUT, GVL, GVLP, I, M, A, P, PM). Never deletes non-gittable objects
    like Task Configuration, Library Manager, etc.
    This provides idempotency by starting from a clean slate.
    """
    debugPrint("Deleting existing root objects...")
    
    # Get all children of the project
    root_children = list(proj.get_children())
    deleted_count = 0
    for child in root_children:
        try:
            child_name = child.get_name()
            # Only remove objects whose type GUID maps to a known prefix in PREFIX_TO_GUID
            prefix = _guid_to_prefix(child.type)
            if prefix is not None and prefix in PREFIX_TO_GUID:
                debugPrint("  Removing: {} ({})".format(child_name, prefix))
                child.remove()
                deleted_count += 1
        except Exception as e:
            debugPrint("  Could not remove {}: {}".format(
                child.get_name() if hasattr(child, 'get_name') else '?', e))
    
    print("Removed {} existing root objects".format(deleted_count))


def _guid_to_prefix(guid):
    """Convert a GUID to a prefix string, or None if not recognized."""
    guid_str = str(guid) if guid else ''
    for prefix, g in PREFIX_TO_GUID.items():
        if str(g) == guid_str:
            return prefix
    # Check folder
    if str(folderGUID) == guid_str:
        return 'FOLDER'
    return None


############################################################################
################################# MAIN
############################################################################


def main():
    """Main entry point. Opens project, discovers tree, deletes existing,
    imports new objects, and compiles."""
    print("CodesysClearForCI.py - Delete all POUs and folders from POU Tab")
    print("")

    # Set target Project to primary
    proj = projects.primary

    print("Removing existing importable objects...")
    delete_existing_root_objects(proj)
    print("")

    print("DONE! Script runtime: {}".format(datetime.now() - start))


if __name__ == "__main__":
    main()
