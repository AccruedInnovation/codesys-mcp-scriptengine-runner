# -*- coding: utf-8 -*-
from __future__ import print_function

import os
import clr
clr.AddReference("System")

from System.Reflection import Assembly, BindingFlags
from System.Threading import Thread

ASSEMBLY_FILE = "AccruedInnovation.CodesysMcp.ScriptRunner.dll"
TYPE_NAME = "AccruedInnovation.CodesysMcp.ScriptRunner.RuntimeRegistration"


def _script_directory():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except Exception:
        return os.getcwd()


def _format_exception_chain(exc):
    lines = []
    current = exc
    depth = 0
    while current is not None and depth < 12:
        try:
            typename = current.GetType().FullName
        except Exception:
            typename = type(current).__name__
        try:
            message = str(current.Message)
        except Exception:
            message = str(current)
        lines.append("%s: %s" % (typename, message))
        try:
            current = current.InnerException
        except Exception:
            current = None
        depth += 1
    return "\n  caused by: ".join(lines)


def invoke(method_name):
    script_dir = _script_directory()
    dll_path = os.path.join(script_dir, ASSEMBLY_FILE)

    if not os.path.isfile(dll_path):
        raise IOError("Script runner assembly was not found: " + dll_path)

    print("Loading: " + dll_path)
    assembly = Assembly.LoadFrom(dll_path)
    runtime_type = assembly.GetType(TYPE_NAME, True)
    method = runtime_type.GetMethod(
        method_name,
        BindingFlags.Public | BindingFlags.Static)
    if method is None:
        raise RuntimeError("Method was not found: " + method_name)

    try:
        result = method.Invoke(None, None)
    except Exception as exc:
        raise RuntimeError(_format_exception_chain(exc))

    print(str(result))
    return result


def wait_for_manifest_update(milliseconds=500):
    # CODESYS reports Thread.Sleep as a warning when the script runs on its STA
    # thread. Join with a timeout is the ScriptEngine-recommended wait form.
    Thread.CurrentThread.Join(milliseconds)
