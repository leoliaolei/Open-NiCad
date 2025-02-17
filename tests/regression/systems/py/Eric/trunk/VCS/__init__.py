# -*- coding: utf-8 -*-

# Copyright (c) 2002 - 2010 Detlev Offenbach <detlev@die-offenbachs.de>
#

"""
Module implementing the general part of the interface to version control systems.

The general part of the VCS interface defines classes to implement common
dialogs. These are a dialog to enter command options, a dialog to display
some repository information and an abstract base class. The individual 
interfaces (i.e. CVS) have to be subclasses of this base class.
"""

from KdeQt.KQApplication import e4App

import Preferences

######################################################################
## Below is the factory function to instantiate the appropriate
## vcs object depending on the project settings.
######################################################################

def factory(vcs):
    """
    Modul factory function to generate the right vcs object.
    
    @param vcs name of the VCS system to be used (string)
    @return the instantiated VCS object
    """
    pluginManager = e4App().getObject("PluginManager")
    if pluginManager is None:
        # that should not happen
        vc = None
    
    vc = pluginManager.getPluginObject("version_control", vcs, maybeActive = True)
    if vc is None:
        # try alternative vcs interfaces assuming, that there is a common
        # indicator for the alternatives
        found = False
        for indicator, vcsData in pluginManager.getVcsSystemIndicators().items():
            for vcsSystem, vcsSystemDisplay in vcsData:
                if vcsSystem == vcs:
                    found = True
                    break
            
            if found:
                if len(vcsData) > 1:
                    for vcsSystem, vcsSystemDisplay in vcsData:
                        if vcsSystem != vcs:
                            vc = pluginManager.getPluginObject(
                                "version_control", vcsSystem, maybeActive = True)
                            if vc is not None:
                                break
                break
    return vc
