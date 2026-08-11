# -*- coding: utf-8 -*-
from __future__ import print_function
import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

from codesys_mcp_script_runner_common import invoke
invoke("UnregisterScriptRunner")
