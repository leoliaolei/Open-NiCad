# -*- coding: utf-8 -*-

# Copyright (c) 2007 - 2010 Detlev Offenbach <detlev@die-offenbachs.de>
#

"""
Module implementing the debug server.
"""

import sys
import os
import time
import signal

from PyQt4.QtCore import *
from PyQt4.QtGui import QMessageBox
from PyQt4.QtNetwork import QTcpServer, QHostAddress, QHostInfo

from KdeQt import KQMessageBox
from KdeQt.KQApplication import e4App

from BreakPointModel import BreakPointModel
from WatchPointModel import WatchPointModel
import DebugClientCapabilities

import Preferences
import Utilities


DebuggerInterfaces = [
    "DebuggerInterfacePython",
    "DebuggerInterfacePython3",
    "DebuggerInterfaceRuby",
    "DebuggerInterfaceNone",
]

class DebugServer(QTcpServer):
    """
    Class implementing the debug server embedded within the IDE.
    
    @signal clientProcessStdout emitted after the client has sent some output
            via stdout
    @signal clientProcessStderr emitted after the client has sent some output
            via stderr
    @signal clientOutput emitted after the client has sent some output
    @signal clientRawInputSent emitted after the data was sent to the debug client
    @signal clientLine(filename, lineno, forStack) emitted after the debug client 
            has executed a line of code
    @signal clientStack(stack) emitted after the debug client has executed a
            line of code
    @signal clientThreadList(currentId, threadList) emitted after a thread list
            has been received
    @signal clientThreadSet emitted after the client has acknowledged the change
            of the current thread
    @signal clientVariables(scope, variables) emitted after a variables dump has 
            been received
    @signal clientVariable(scope, variables) emitted after a dump for one class 
            variable has been received
    @signal clientStatement(boolean) emitted after an interactive command has
            been executed. The parameter is 0 to indicate that the command is
            complete and 1 if it needs more input.
    @signal clientException(exception) emitted after an exception occured on the 
            client side
    @signal clientSyntaxError(exception) emitted after a syntax error has been detected
            on the client side
    @signal clientExit(int) emitted with the exit status after the client has exited
    @signal clientClearBreak(filename, lineno) emitted after the debug client
            has decided to clear a temporary breakpoint
    @signal clientBreakConditionError(fn, lineno) emitted after the client has signaled
            a syntax error in a breakpoint condition
    @signal clientClearWatch(condition) emitted after the debug client
            has decided to clear a temporary watch expression
    @signal clientWatchConditionError(condition) emitted after the client has signaled
            a syntax error in a watch expression
    @signal clientRawInput(prompt, echo) emitted after a raw input request was received
    @signal clientBanner(banner) emitted after the client banner was received
    @signal clientCapabilities(int capabilities, QString cltype) emitted after the clients
            capabilities were received
    @signal clientCompletionList(completionList, text) emitted after the client
            the commandline completion list and the reworked searchstring was
            received from the client
    @signal passiveDebugStarted emitted after the debug client has connected in
            passive debug mode
    @signal clientGone emitted if the client went away (planned or unplanned)
    @signal utPrepared(nrTests, exc_type, exc_value) emitted after the client has
            loaded a unittest suite
    @signal utFinished emitted after the client signalled the end of the unittest
    @signal utStartTest(testname, testdocu) emitted after the client has started 
            a test
    @signal utStopTest emitted after the client has finished a test
    @signal utTestFailed(testname, exc_info) emitted after the client reported 
            a failed test
    @signal utTestErrored(testname, exc_info) emitted after the client reported 
            an errored test
    """
    def __init__(self):
        """
        Constructor
        """
        QTcpServer.__init__(self)
        
        # create our models
        self.breakpointModel = BreakPointModel(self)
        self.watchpointModel = WatchPointModel(self)
        self.watchSpecialCreated = \
            self.trUtf8("created", "must be same as in EditWatchpointDialog") 
        self.watchSpecialChanged = \
            self.trUtf8("changed", "must be same as in EditWatchpointDialog")
        
        self.networkInterface = unicode(Preferences.getDebugger("NetworkInterface"))
        if self.networkInterface == "all":
            hostAddress = QHostAddress(QHostAddress.Any)
        elif self.networkInterface == "allv6":
            hostAddress = QHostAddress(QHostAddress.AnyIPv6)
        else:
            hostAddress = QHostAddress(Preferences.getDebugger("NetworkInterface"))
        if Preferences.getDebugger("PassiveDbgEnabled"):
            socket = Preferences.getDebugger("PassiveDbgPort") # default: 42424
            self.listen(hostAddress, socket)
            self.passive = True
            self.passiveClientExited = False
        else:
            self.listen(hostAddress)
            self.passive = False
        
        self.debuggerInterface = None
        self.debugging = False
        self.clientProcess = None
        self.clientType = Preferences.Prefs.settings.value(\
            'DebugClient/Type', QVariant('Python')).toString()
        self.lastClientType = ''
        self.__autoClearShell = False
        
        self.connect(self, SIGNAL("clientClearBreak"), self.__clientClearBreakPoint)
        self.connect(self, SIGNAL("clientClearWatch"), self.__clientClearWatchPoint)
        self.connect(self, SIGNAL("newConnection()"), self.__newConnection)
        
        self.connect(self.breakpointModel, 
            SIGNAL("rowsAboutToBeRemoved(const QModelIndex &, int, int)"), 
            self.__deleteBreakPoints)
        self.connect(self.breakpointModel,
            SIGNAL("dataAboutToBeChanged(const QModelIndex &, const QModelIndex &)"),
            self.__breakPointDataAboutToBeChanged)
        self.connect(self.breakpointModel,
            SIGNAL("dataChanged(const QModelIndex &, const QModelIndex &)"),
            self.__changeBreakPoints)
        self.connect(self.breakpointModel,
            SIGNAL("rowsInserted(const QModelIndex &, int, int)"),
            self.__addBreakPoints)
        
        self.connect(self.watchpointModel, 
            SIGNAL("rowsAboutToBeRemoved(const QModelIndex &, int, int)"), 
            self.__deleteWatchPoints)
        self.connect(self.watchpointModel,
            SIGNAL("dataAboutToBeChanged(const QModelIndex &, const QModelIndex &)"),
            self.__watchPointDataAboutToBeChanged)
        self.connect(self.watchpointModel, 
            SIGNAL("dataChanged(const QModelIndex &, const QModelIndex &)"),
            self.__changeWatchPoints)
        self.connect(self.watchpointModel,
            SIGNAL("rowsInserted(const QModelIndex &, int, int)"),
            self.__addWatchPoints)
        
        self.__registerDebuggerInterfaces()
        
    def getHostAddress(self, localhost):
        """
        Public method to get the IP address or hostname the debug server is listening.
        
        @param localhost flag indicating to return the address for localhost (boolean)
        @return IP address or hostname (string)
        """
        if self.networkInterface == "all":
            if localhost:
                return "127.0.0.1"
            else:
                return "%s@@v4" % QHostInfo.localHostName()
        elif self.networkInterface == "allv6":
            if localhost:
                return "::1"
            else:
                return "%s@@v6" % QHostInfo.localHostName()
        else:
            return self.networkInterface
        
    def preferencesChanged(self):
        """
        Public slot to handle the preferencesChanged signal.
        """
        self.__registerDebuggerInterfaces()
        
    def __registerDebuggerInterfaces(self):
        """
        Private method to register the available debugger interface modules.
        """
        self.__clientCapabilities = {}
        self.__clientAssociations = {}
        
        for interface in DebuggerInterfaces:
            modName = "Debugger.%s" % interface
            mod = __import__(modName)
            components = modName.split('.')
            for comp in components[1:]:
                mod = getattr(mod, comp)
            
            clientLanguage, clientCapabilities, clientExtensions = \
                mod.getRegistryData()
            if clientLanguage:
                self.__clientCapabilities[clientLanguage] = clientCapabilities
                for extension in clientExtensions:
                    if extension not in self.__clientAssociations:
                        self.__clientAssociations[extension] = clientLanguage
        
    def getSupportedLanguages(self, shellOnly = False):
        """
        Public slot to return the supported programming languages.
        
        @param shellOnly flag indicating only languages supporting an
            interactive shell should be returned
        @return list of supported languages (list of strings)
        """
        languages = self.__clientCapabilities.keys()
        try:
            del languages[languages.index("None")]
        except ValueError:
            pass    # it is not in the list
        
        if shellOnly:
            languages = \
                [lang for lang in languages \
                 if self.__clientCapabilities[lang] & DebugClientCapabilities.HasShell]
        
        return languages[:]
        
    def getExtensions(self, language):
        """
        Public slot to get the extensions associated with the given language.
        
        @param language language to get extensions for (string)
        @return tuple of extensions associated with the language (tuple of strings)
        """
        extensions = []
        for ext, lang in self.__clientAssociations.items():
            if lang == language:
                extensions.append(ext)
        
        return tuple(extensions)
        
    def __createDebuggerInterface(self, clientType = None):
        """
        Private slot to create the debugger interface object.
        
        @param clientType type of the client interface to be created (string or QString)
        """
        if self.lastClientType != self.clientType or clientType is not None:
            if clientType is None:
                clientType = self.clientType
            if clientType == "Python":
                from DebuggerInterfacePython import DebuggerInterfacePython
                self.debuggerInterface = DebuggerInterfacePython(self, self.passive)
            elif clientType == "Python3":
                from DebuggerInterfacePython3 import DebuggerInterfacePython3
                self.debuggerInterface = DebuggerInterfacePython3(self, self.passive)
            elif clientType == "Ruby":
                from DebuggerInterfaceRuby import DebuggerInterfaceRuby
                self.debuggerInterface = DebuggerInterfaceRuby(self, self.passive)
            elif clientType == "None":
                from DebuggerInterfaceNone import DebuggerInterfaceNone
                self.debuggerInterface = DebuggerInterfaceNone(self, self.passive)
            else:
                from DebuggerInterfaceNone import DebuggerInterfaceNone
                self.debuggerInterface = DebuggerInterfaceNone(self, self.passive)
                self.clientType = "None"
        
    def __setClientType(self, clType):
        """
        Private method to set the client type.
        
        @param clType type of client to be started (string)
        """
        if clType is not None and clType in self.getSupportedLanguages():
            self.clientType = clType
            ok = Preferences.Prefs.settings.setValue('DebugClient/Type', 
                QVariant(self.clientType))
        
    def startClient(self, unplanned = True, clType = None, forProject = False, 
                    runInConsole = False):
        """
        Public method to start a debug client.
        
        @keyparam unplanned flag indicating that the client has died (boolean)
        @keyparam clType type of client to be started (string)
        @keyparam forProject flag indicating a project related action (boolean)
        @keyparam runInConsole flag indicating to start the debugger in a 
            console window (boolean)
        """
        if not self.passive or not self.passiveClientExited: 
            if self.debuggerInterface and self.debuggerInterface.isConnected():
                self.shutdownServer()
                self.emit(SIGNAL('clientGone'), unplanned & self.debugging)
        
        if clType:
            self.__setClientType(clType)
        
        # only start the client, if we are not in passive mode
        if not self.passive:
            if self.clientProcess:
                self.disconnect(self.clientProcess, SIGNAL("readyReadStandardError()"), 
                                self.__clientProcessError)
                self.disconnect(self.clientProcess, SIGNAL("readyReadStandardOutput()"), 
                                self.__clientProcessOutput)
                self.clientProcess.close()
                self.clientProcess.kill()
                self.clientProcess.waitForFinished(10000)
                self.clientProcess = None
            
            self.__createDebuggerInterface()
            if forProject:
                project = e4App().getObject("Project")
                if not project.isDebugPropertiesLoaded():
                    self.clientProcess, isNetworked = \
                        self.debuggerInterface.startRemote(self.serverPort(), 
                                                           runInConsole)
                else:
                    self.clientProcess, isNetworked = \
                        self.debuggerInterface.startRemoteForProject(self.serverPort(), 
                                                                     runInConsole)
            else:
                self.clientProcess, isNetworked = \
                    self.debuggerInterface.startRemote(self.serverPort(), runInConsole)
            
            if self.clientProcess:
                self.connect(self.clientProcess, SIGNAL("readyReadStandardError()"), 
                             self.__clientProcessError)
                self.connect(self.clientProcess, SIGNAL("readyReadStandardOutput()"), 
                             self.__clientProcessOutput)
                
                if not isNetworked:
                    # the client is connected through stdin and stdout
                    # Perform actions necessary, if client type has changed
                    if self.lastClientType != self.clientType:
                        self.lastClientType = self.clientType
                        self.remoteBanner()
                    elif self.__autoClearShell:
                        self.__autoClearShell = False
                        self.remoteBanner()
                    
                    self.debuggerInterface.flush()
        else:
            self.__createDebuggerInterface("None")

    def __clientProcessOutput(self):
        """
        Private slot to process client output received via stdout.
        """
        output = QString(self.clientProcess.readAllStandardOutput())
        self.emit(SIGNAL("clientProcessStdout"), output)
        
    def __clientProcessError(self):
        """
        Private slot to process client output received via stderr.
        """
        error = QString(self.clientProcess.readAllStandardError())
        self.emit(SIGNAL("clientProcessStderr"), error)
        
    def __clientClearBreakPoint(self, fn, lineno):
        """
        Private slot to handle the clientClearBreak signal.
        
        @param fn filename of breakpoint to clear (string or QString)
        @param lineno line number of breakpoint to clear (integer)
        """
        if self.debugging:
            index = self.breakpointModel.getBreakPointIndex(fn, lineno)
            self.breakpointModel.deleteBreakPointByIndex(index)

    def __deleteBreakPoints(self, parentIndex, start, end):
        """
        Private slot to delete breakpoints.
        
        @param parentIndex index of parent item (QModelIndex)
        @param start start row (integer)
        @param end end row (integer)
        """
        if self.debugging:
            for row in range(start, end+1):
                index = self.breakpointModel.index(row, 0, parentIndex)
                fn, lineno = self.breakpointModel.getBreakPointByIndex(index)[0:2]
                self.remoteBreakpoint(fn, lineno, False)

    def __changeBreakPoints(self, startIndex, endIndex):
        """
        Private slot to set changed breakpoints.
        
        @param indexes indexes of changed breakpoints.
        """
        if self.debugging:
            self.__addBreakPoints(QModelIndex(), startIndex.row(), endIndex.row())

    def __breakPointDataAboutToBeChanged(self, startIndex, endIndex):
        """
        Private slot to handle the dataAboutToBeChanged signal of the breakpoint model.
        
        @param startIndex start index of the rows to be changed (QModelIndex)
        @param endIndex end index of the rows to be changed (QModelIndex)
        """
        if self.debugging:
            self.__deleteBreakPoints(QModelIndex(), startIndex.row(), endIndex.row())
        
    def __addBreakPoints(self, parentIndex, start, end):
        """
        Private slot to add breakpoints.
        
        @param parentIndex index of parent item (QModelIndex)
        @param start start row (integer)
        @param end end row (integer)
        """
        if self.debugging:
            for row in range(start, end+1):
                index = self.breakpointModel.index(row, 0, parentIndex)
                fn, line, cond, temp, enabled, ignorecount = \
                    self.breakpointModel.getBreakPointByIndex(index)[:6]
                self.remoteBreakpoint(fn, line, True, cond, temp)
                if not enabled:
                    self.__remoteBreakpointEnable(fn, line, False)
                if ignorecount:
                    self.__remoteBreakpointIgnore(fn, line, ignorecount)

    def __makeWatchCondition(self, cond, special):
        """
        Private method to construct the condition string.
        
        @param cond condition (string or QString)
        @param special special condition (string or QString)
        @return condition string (QString)
        """
        special = unicode(special)
        if special == "":
            _cond = unicode(cond)
        else:
            if special == unicode(self.watchSpecialCreated):
                _cond = "%s ??created??" % cond
            elif special == unicode(self.watchSpecialChanged):
                _cond = "%s ??changed??" % cond
        return _cond
        
    def __splitWatchCondition(self, cond):
        """
        Private method to split a remote watch expression.
        
        @param cond remote expression (string or QString)
        @return tuple of local expression (string) and special condition (string)
        """
        cond = unicode(cond)
        if cond.endswith(" ??created??"):
            cond, special = cond.split()
            special = unicode(self.watchSpecialCreated)
        elif cond.endswith(" ??changed??"):
            cond, special = cond.split()
            special = unicode(self.watchSpecialChanged)
        else:
            return cond, ""
        
    def __clientClearWatchPoint(self, condition):
        """
        Private slot to handle the clientClearWatch signal.
        
        @param condition expression of watch expression to clear (string or QString)
        """
        if self.debugging:
            cond, special = self.__splitWatchCondition(condition)
            index = self.watchpointModel.getWatchPointIndex(cond, special)
            self.watchpointModel.deleteWatchPointByIndex(index)
        
    def __deleteWatchPoints(self, parentIndex, start, end):
        """
        Private slot to delete watch expressions.
        
        @param parentIndex index of parent item (QModelIndex)
        @param start start row (integer)
        @param end end row (integer)
        """
        if self.debugging:
            for row in range(start, end+1):
                index = self.watchpointModel.index(row, 0, parentIndex)
                cond, special = self.watchpointModel.getWatchPointByIndex(index)[0:2]
                cond = self.__makeWatchCondition(cond, special)
                self.__remoteWatchpoint(cond, False)
        
    def __watchPointDataAboutToBeChanged(self, startIndex, endIndex):
        """
        Private slot to handle the dataAboutToBeChanged signal of the 
        watch expression model.
        
        @param startIndex start index of the rows to be changed (QModelIndex)
        @param endIndex end index of the rows to be changed (QModelIndex)
        """
        if self.debugging:
            self.__deleteWatchPoints(QModelIndex(), startIndex.row(), endIndex.row())
        
    def __addWatchPoints(self, parentIndex, start, end):
        """
        Private slot to set a watch expression.
        
        @param parentIndex index of parent item (QModelIndex)
        @param start start row (integer)
        @param end end row (integer)
        """
        if self.debugging:
            for row in range(start, end+1):
                index = self.watchpointModel.index(row, 0, parentIndex)
                cond, special, temp, enabled, ignorecount = \
                    self.watchpointModel.getWatchPointByIndex(index)[:5]
                cond = self.__makeWatchCondition(cond, special)
                self.__remoteWatchpoint(cond, True, temp)
                if not enabled:
                    self.__remoteWatchpointEnable(cond, False)
                if ignorecount:
                    self.__remoteWatchpointIgnore(cond, ignorecount)
        
    def __changeWatchPoints(self, startIndex, endIndex):
        """
        Private slot to set changed watch expressions.
        
        @param startIndex start index of the rows to be changed (QModelIndex)
        @param endIndex end index of the rows to be changed (QModelIndex)
        """
        if self.debugging:
            self.__addWatchPoints(QModelIndex(), startIndex.row(), endIndex.row())
        
    def getClientCapabilities(self, type):
        """
        Public method to retrieve the debug clients capabilities.
        
        @param type debug client type (string)
        @return debug client capabilities (integer)
        """
        try:
            return self.__clientCapabilities[type]
        except KeyError:
            return 0    # no capabilities
        
    def __newConnection(self):
        """
        Private slot to handle a new connection.
        """
        sock = self.nextPendingConnection()
        peerAddress = sock.peerAddress().toString()
        if Preferences.getDebugger("AllowedHosts").indexOf(peerAddress) == -1:
            # the peer is not allowed to connect
            res = KQMessageBox.warning(None,
                self.trUtf8("Connection from illegal host"),
                self.trUtf8("""<p>A connection was attempted by the"""
                    """ illegal host <b>%1</b>. Accept this connection?</p>""")\
                    .arg(peerAddress),
                QMessageBox.StandardButtons(\
                    QMessageBox.No | \
                    QMessageBox.Yes),
                QMessageBox.No)
            if res == QMessageBox.No:
                sock.abort()
                return
            else:
                allowedHosts = Preferences.getDebugger("AllowedHosts")
                allowedHosts.append(peerAddress)
                Preferences.setDebugger("AllowedHosts", allowedHosts)
        
        if self.passive:
            self.__createDebuggerInterface(Preferences.getDebugger("PassiveDbgType"))
        
        accepted = self.debuggerInterface.newConnection(sock)
        if accepted:
            # Perform actions necessary, if client type has changed
            if self.lastClientType != self.clientType:
                self.lastClientType = self.clientType
                self.remoteBanner()
            elif self.__autoClearShell:
                self.__autoClearShell = False
                self.remoteBanner()
            elif self.passive:
                self.remoteBanner()
            
            self.debuggerInterface.flush()

    def shutdownServer(self):
        """
        Public method to cleanly shut down.
        
        It closes our socket and shuts down
        the debug client. (Needed on Win OS)
        """
        if self.debuggerInterface is not None:
            self.debuggerInterface.shutdown()

    def remoteEnvironment(self, env):
        """
        Public method to set the environment for a program to debug, run, ...
        
        @param env environment settings (string)
        """
        envlist = Utilities.parseEnvironmentString(env)
        envdict = {}
        for el in envlist:
            try:
                key, value = el.split('=', 1)
                if value.startswith('"') or value.startswith("'"):
                    value = value[1:-1]
                envdict[unicode(key)] = unicode(value)
            except UnpackError:
                pass
        self.debuggerInterface.remoteEnvironment(envdict)
        
    def remoteLoad(self, fn, argv, wd, env, autoClearShell = True,
                   tracePython = False, autoContinue = True, forProject = False, 
                   runInConsole = False, autoFork = False, forkChild = False):
        """
        Public method to load a new program to debug.
        
        @param fn the filename to debug (string)
        @param argv the commandline arguments to pass to the program (string or QString)
        @param wd the working directory for the program (string)
        @param env environment settings (string)
        @keyparam autoClearShell flag indicating, that the interpreter window should
            be cleared (boolean)
        @keyparam tracePython flag indicating if the Python library should be traced
            as well (boolean)
        @keyparam autoContinue flag indicating, that the debugger should not stop
            at the first executable line (boolean)
        @keyparam forProject flag indicating a project related action (boolean)
        @keyparam runInConsole flag indicating to start the debugger in a 
            console window (boolean)
        @keyparam autoFork flag indicating the automatic fork mode (boolean)
        @keyparam forkChild flag indicating to debug the child after forking (boolean)
        """
        self.__autoClearShell = autoClearShell
        self.__autoContinue = autoContinue
        
        # Restart the client
        try:
            self.__setClientType(self.__clientAssociations[os.path.splitext(fn)[1]])
        except KeyError:
            self.__setClientType('Python')    # assume it is a Python file
        self.startClient(False, forProject = forProject, runInConsole = runInConsole)
        
        self.remoteEnvironment(env)
        
        self.debuggerInterface.remoteLoad(fn, argv, wd, tracePython, autoContinue, 
                                          autoFork, forkChild)
        self.debugging = True
        self.__restoreBreakpoints()
        self.__restoreWatchpoints()

    def remoteRun(self, fn, argv, wd, env, autoClearShell = True,
                  forProject = False, runInConsole = False):
        """
        Public method to load a new program to run.
        
        @param fn the filename to run (string)
        @param argv the commandline arguments to pass to the program (string or QString)
        @param wd the working directory for the program (string)
        @param env environment settings (string)
        @keyparam autoClearShell flag indicating, that the interpreter window should
            be cleared (boolean)
        @keyparam forProject flag indicating a project related action (boolean)
        @keyparam runInConsole flag indicating to start the debugger in a 
            console window (boolean)
        """
        self.__autoClearShell = autoClearShell
        
        # Restart the client
        try:
            self.__setClientType(self.__clientAssociations[os.path.splitext(fn)[1]])
        except KeyError:
            self.__setClientType('Python')    # assume it is a Python file
        self.startClient(False, forProject = forProject, runInConsole = runInConsole)
        
        self.remoteEnvironment(env)
        
        self.debuggerInterface.remoteRun(fn, argv, wd)
        self.debugging = False

    def remoteCoverage(self, fn, argv, wd, env, autoClearShell = True,
                       erase = False, forProject = False, runInConsole = False):
        """
        Public method to load a new program to collect coverage data.
        
        @param fn the filename to run (string)
        @param argv the commandline arguments to pass to the program (string or QString)
        @param wd the working directory for the program (string)
        @param env environment settings (string)
        @keyparam autoClearShell flag indicating, that the interpreter window should
            be cleared (boolean)
        @keyparam erase flag indicating that coverage info should be 
            cleared first (boolean)
        @keyparam forProject flag indicating a project related action (boolean)
        @keyparam runInConsole flag indicating to start the debugger in a 
            console window (boolean)
        """
        self.__autoClearShell = autoClearShell
        
        # Restart the client
        try:
            self.__setClientType(self.__clientAssociations[os.path.splitext(fn)[1]])
        except KeyError:
            self.__setClientType('Python')    # assume it is a Python file
        self.startClient(False, forProject = forProject, runInConsole = runInConsole)
        
        self.remoteEnvironment(env)
        
        self.debuggerInterface.remoteCoverage(fn, argv, wd, erase)
        self.debugging = False

    def remoteProfile(self, fn, argv, wd, env, autoClearShell = True,
                      erase = False, forProject = False, 
                      runInConsole = False):
        """
        Public method to load a new program to collect profiling data.
        
        @param fn the filename to run (string)
        @param argv the commandline arguments to pass to the program (string or QString)
        @param wd the working directory for the program (string)
        @param env environment settings (string)
        @keyparam autoClearShell flag indicating, that the interpreter window should
            be cleared (boolean)
        @keyparam erase flag indicating that timing info should be cleared first (boolean)
        @keyparam forProject flag indicating a project related action (boolean)
        @keyparam runInConsole flag indicating to start the debugger in a 
            console window (boolean)
        """
        self.__autoClearShell = autoClearShell
        
        # Restart the client
        try:
            self.__setClientType(self.__clientAssociations[os.path.splitext(fn)[1]])
        except KeyError:
            self.__setClientType('Python')    # assume it is a Python file
        self.startClient(False, forProject = forProject, runInConsole = runInConsole)
        
        self.remoteEnvironment(env)
        
        self.debuggerInterface.remoteProfile(fn, argv, wd, erase)
        self.debugging = False

    def remoteStatement(self, stmt):
        """
        Public method to execute a Python statement.  
        
        @param stmt the Python statement to execute (string). It
              should not have a trailing newline.
        """
        self.debuggerInterface.remoteStatement(stmt)

    def remoteStep(self):
        """
        Public method to single step the debugged program.
        """
        self.debuggerInterface.remoteStep()

    def remoteStepOver(self):
        """
        Public method to step over the debugged program.
        """
        self.debuggerInterface.remoteStepOver()

    def remoteStepOut(self):
        """
        Public method to step out the debugged program.
        """
        self.debuggerInterface.remoteStepOut()

    def remoteStepQuit(self):
        """
        Public method to stop the debugged program.
        """
        self.debuggerInterface.remoteStepQuit()

    def remoteContinue(self, special = False):
        """
        Public method to continue the debugged program.
        
        @param special flag indicating a special continue operation
        """
        self.debuggerInterface.remoteContinue(special)

    def remoteBreakpoint(self, fn, line, set, cond=None, temp=False):
        """
        Public method to set or clear a breakpoint.
        
        @param fn filename the breakpoint belongs to (string)
        @param line linenumber of the breakpoint (int)
        @param set flag indicating setting or resetting a breakpoint (boolean)
        @param cond condition of the breakpoint (string)
        @param temp flag indicating a temporary breakpoint (boolean)
        """
        self.debuggerInterface.remoteBreakpoint(fn, line, set, cond, temp)
        
    def __remoteBreakpointEnable(self, fn, line, enable):
        """
        Private method to enable or disable a breakpoint.
        
        @param fn filename the breakpoint belongs to (string)
        @param line linenumber of the breakpoint (int)
        @param enable flag indicating enabling or disabling a breakpoint (boolean)
        """
        self.debuggerInterface.remoteBreakpointEnable(fn, line, enable)
        
    def __remoteBreakpointIgnore(self, fn, line, count):
        """
        Private method to ignore a breakpoint the next couple of occurrences.
        
        @param fn filename the breakpoint belongs to (string)
        @param line linenumber of the breakpoint (int)
        @param count number of occurrences to ignore (int)
        """
        self.debuggerInterface.remoteBreakpointIgnore(fn, line, count)
        
    def __remoteWatchpoint(self, cond, set, temp = False):
        """
        Private method to set or clear a watch expression.
        
        @param cond expression of the watch expression (string)
        @param set flag indicating setting or resetting a watch expression (boolean)
        @param temp flag indicating a temporary watch expression (boolean)
        """
        # cond is combination of cond and special (s. watch expression viewer)
        self.debuggerInterface.remoteWatchpoint(cond, set, temp)
    
    def __remoteWatchpointEnable(self, cond, enable):
        """
        Private method to enable or disable a watch expression.
        
        @param cond expression of the watch expression (string)
        @param enable flag indicating enabling or disabling a watch expression (boolean)
        """
        # cond is combination of cond and special (s. watch expression viewer)
        self.debuggerInterface.remoteWatchpointEnable(cond, enable)
    
    def __remoteWatchpointIgnore(self, cond, count):
        """
        Private method to ignore a watch expression the next couple of occurrences.
        
        @param cond expression of the watch expression (string)
        @param count number of occurrences to ignore (int)
        """
        # cond is combination of cond and special (s. watch expression viewer)
        self.debuggerInterface.remoteWatchpointIgnore(cond, count)
    
    def remoteRawInput(self,s):
        """
        Public method to send the raw input to the debugged program.
        
        @param s the raw input (string)
        """
        self.debuggerInterface.remoteRawInput(s)
        self.emit(SIGNAL('clientRawInputSent'))
        
    def remoteThreadList(self):
        """
        Public method to request the list of threads from the client.
        """
        self.debuggerInterface.remoteThreadList()
        
    def remoteSetThread(self, tid):
        """
        Public method to request to set the given thread as current thread.
        
        @param tid id of the thread (integer)
        """
        self.debuggerInterface.remoteSetThread(tid)
        
    def remoteClientVariables(self, scope, filter, framenr = 0):
        """
        Public method to request the variables of the debugged program.
        
        @param scope the scope of the variables (0 = local, 1 = global)
        @param filter list of variable types to filter out (list of int)
        @param framenr framenumber of the variables to retrieve (int)
        """
        self.debuggerInterface.remoteClientVariables(scope, filter, framenr)
        
    def remoteClientVariable(self, scope, filter, var, framenr = 0):
        """
        Public method to request the variables of the debugged program.
        
        @param scope the scope of the variables (0 = local, 1 = global)
        @param filter list of variable types to filter out (list of int)
        @param var list encoded name of variable to retrieve (string)
        @param framenr framenumber of the variables to retrieve (int)
        """
        self.debuggerInterface.remoteClientVariable(scope, filter, var, framenr)
        
    def remoteClientSetFilter(self, scope, filter):
        """
        Public method to set a variables filter list.
        
        @param scope the scope of the variables (0 = local, 1 = global)
        @param filter regexp string for variable names to filter out (string)
        """
        self.debuggerInterface.remoteClientSetFilter(scope, filter)
        
    def remoteEval(self, arg):
        """
        Public method to evaluate arg in the current context of the debugged program.
        
        @param arg the arguments to evaluate (string)
        """
        self.debuggerInterface.remoteEval(arg)
        
    def remoteExec(self, stmt):
        """
        Public method to execute stmt in the current context of the debugged program.
        
        @param stmt statement to execute (string)
        """
        self.debuggerInterface.remoteExec(stmt)
        
    def remoteBanner(self):
        """
        Public slot to get the banner info of the remote client.
        """
        self.debuggerInterface.remoteBanner()
        
    def remoteCapabilities(self):
        """
        Public slot to get the debug clients capabilities.
        """
        self.debuggerInterface.remoteCapabilities()
        
    def remoteCompletion(self, text):
        """
        Public slot to get the a list of possible commandline completions
        from the remote client.
        
        @param text the text to be completed (string or QString)
        """
        self.debuggerInterface.remoteCompletion(text)

    def remoteUTPrepare(self, fn, tn, tfn, cov, covname, coverase):
        """
        Public method to prepare a new unittest run.
        
        @param fn the filename to load (string)
        @param tn the testname to load (string)
        @param tfn the test function name to load tests from (string)
        @param cov flag indicating collection of coverage data is requested
        @param covname filename to be used to assemble the coverage caches
                filename
        @param coverase flag indicating erasure of coverage data is requested
        """
        # Restart the client if there is already a program loaded.
        try:
            self.__setClientType(self.__clientAssociations[os.path.splitext(fn)[1]])
        except KeyError:
            self.__setClientType('Python')    # assume it is a Python file
        self.startClient(False)
        
        self.debuggerInterface.remoteUTPrepare(fn, tn, tfn, cov, covname, coverase)
        self.debugging = False
        
    def remoteUTRun(self):
        """
        Public method to start a unittest run.
        """
        self.debuggerInterface.remoteUTRun()
        
    def remoteUTStop(self):
        """
        public method to stop a unittest run.
        """
        self.debuggerInterface.remoteUTStop()
        
    def clientOutput(self, line):
        """
        Public method to process a line of client output.
        
        @param line client output (string)
        """
        self.emit(SIGNAL('clientOutput'), line)
        
    def clientLine(self, filename, lineno, forStack = False):
        """
        Public method to process client position feedback.
        
        @param filename name of the file currently being executed (string)
        @param lineno line of code currently being executed (integer)
        @param forStack flag indicating this is for a stack dump (boolean)
        """
        self.emit(SIGNAL('clientLine'), filename, lineno, forStack)
        
    def clientStack(self, stack):
        """
        Public method to process a client's stack information.
        
        @param stack list of stack entries. Each entry is a tuple of three
            values giving the filename, linenumber and method
            (list of lists of (string, integer, string))
        """
        self.emit(SIGNAL('clientStack'), stack)
        
    def clientThreadList(self, currentId, threadList):
        """
        Public method to process the client thread list info.
        
        @param currentID id of the current thread (integer)
        @param threadList list of dictionaries containing the thread data
        """
        self.emit(SIGNAL('clientThreadList'), currentId, threadList)
        
    def clientThreadSet(self):
        """
        Public method to handle the change of the client thread.
        """
        self.emit(SIGNAL('clientThreadSet'))
        
    def clientVariables(self, scope, variables):
        """
        Public method to process the client variables info.
        
        @param scope scope of the variables (-1 = empty global, 1 = global, 0 = local)
        @param variables the list of variables from the client
        """
        self.emit(SIGNAL('clientVariables'), scope, variables)
        
    def clientVariable(self, scope, variables):
        """
        Public method to process the client variable info.
        
        @param scope scope of the variables (-1 = empty global, 1 = global, 0 = local)
        @param variables the list of members of a classvariable from the client
        """
        self.emit(SIGNAL('clientVariable'), scope, variables)
        
    def clientStatement(self, more):
        """
        Public method to process the input response from the client.
        
        @param more flag indicating that more user input is required
        """
        self.emit(SIGNAL('clientStatement'), more)
        
    def clientException(self, exceptionType, exceptionMessage, stackTrace):
        """
        Public method to process the exception info from the client.
        
        @param exceptionType type of exception raised (string)
        @param exceptionMessage message given by the exception (string)
        @param stackTrace list of stack entries with the exception position
            first. Each stack entry is a list giving the filename and the linenumber.
        """
        self.emit(SIGNAL('clientException'), exceptionType, exceptionMessage, stackTrace)
        
    def clientSyntaxError(self, message, filename, lineNo, characterNo):
        """
        Public method to process the syntax error info from the client.
        
        @param message message of the syntax error (string)
        @param filename translated filename of the syntax error position (string)
        @param lineNo line number of the syntax error position (integer)
        @param characterNo character number of the syntax error position (integer)
        """
        self.emit(SIGNAL('clientSyntaxError'), message, filename, lineNo, characterNo)
        
    def clientExit(self, status):
        """
        Public method to process the client exit status.
        
        @param status exit code as a string (string)
        """
        if self.passive:
            self.__passiveShutDown()
        self.emit(SIGNAL('clientExit(int)'), int(status))
        if Preferences.getDebugger("AutomaticReset"):
            self.startClient(False)
        if self.passive:
            self.__createDebuggerInterface("None")
            self.clientOutput(self.trUtf8('\nNot connected\n'))
            self.clientStatement(False)
        
    def clientClearBreak(self, filename, lineno):
        """
        Public method to process the client clear breakpoint command.
        
        @param filename filename of the breakpoint (string)
        @param lineno line umber of the breakpoint (integer)
        """
        self.emit(SIGNAL('clientClearBreak'), filename, lineno)
        
    def clientBreakConditionError(self, filename, lineno):
        """
        Public method to process the client breakpoint condition error info.
        
        @param filename filename of the breakpoint (string)
        @param lineno line umber of the breakpoint (integer)
        """
        self.emit(SIGNAL('clientBreakConditionError'), filename, lineno)
        
    def clientClearWatch(self, condition):
        """
        Public slot to handle the clientClearWatch signal.
        
        @param condition expression of watch expression to clear (string or QString)
        """
        self.emit(SIGNAL('clientClearWatch'), condition)
        
    def clientWatchConditionError(self, condition):
        """
        Public method to process the client watch expression error info.
        
        @param condition expression of watch expression to clear (string or QString)
        """
        self.emit(SIGNAL('clientWatchConditionError'), condition)
        
    def clientRawInput(self, prompt, echo):
        """
        Public method to process the client raw input command.
        
        @param prompt the input prompt (string)
        @param echo flag indicating an echoing of the input (boolean)
        """
        self.emit(SIGNAL('clientRawInput'), prompt, echo)
        
    def clientBanner(self, version, platform, debugClient):
        """
        Public method to process the client banner info.
        
        @param version interpreter version info (string)
        @param platform hostname of the client (string)
        @param debugClient additional debugger type info (string)
        """
        self.emit(SIGNAL('clientBanner'), version, platform, debugClient)
        
    def clientCapabilities(self, capabilities, clientType):
        """
        Public method to process the client capabilities info.
        
        @param capabilities bitmaks with the client capabilities (integer)
        @param clientType type of the debug client (string)
        """
        self.__clientCapabilities[clientType] = capabilities
        self.emit(SIGNAL('clientCapabilities'), capabilities, clientType)
        
    def clientCompletionList(self, completionList, text):
        """
        Public method to process the client auto completion info.
        
        @param completionList list of possible completions (list of strings)
        @param text the text to be completed (string)
        """
        self.emit(SIGNAL('clientCompletionList'), completionList, text)
        
    def clientUtPrepared(self, result, exceptionType, exceptionValue):
        """
        Public method to process the client unittest prepared info.
        
        @param result number of test cases (0 = error) (integer)
        @param exceptionType exception type (string)
        @param exceptionValue exception message (string)
        """
        self.emit(SIGNAL('utPrepared'), result, exceptionType, exceptionValue)
        
    def clientUtStartTest(self, testname, doc):
        """
        Public method to process the client start test info.
        
        @param testname name of the test (string)
        @param doc short description of the test (string)
        """
        self.emit(SIGNAL('utStartTest'), testname, doc)
        
    def clientUtStopTest(self):
        """
        Public method to process the client stop test info.
        """
        self.emit(SIGNAL('utStopTest'))
        
    def clientUtTestFailed(self, testname, traceback):
        """
        Public method to process the client test failed info.
        
        @param testname name of the test (string)
        @param traceback lines of traceback info (string)
        """
        self.emit(SIGNAL('utTestFailed'), testname, traceback)
        
    def clientUtTestErrored(self, testname, traceback):
        """
        Public method to process the client test errored info.
        
        @param testname name of the test (string)
        @param traceback lines of traceback info (string)
        """
        self.emit(SIGNAL('utTestErrored'), testname, traceback)
        
    def clientUtFinished(self):
        """
        Public method to process the client unit test finished info.
        """
        self.emit(SIGNAL('utFinished'))
        
    def passiveStartUp(self, fn, exc):
        """
        Public method to handle a passive debug connection.
        
        @param fn filename of the debugged script (string)
        @param exc flag to enable exception reporting of the IDE (boolean)
        """
        print unicode(self.trUtf8("Passive debug connection received"))
        self.passiveClientExited = False
        self.debugging = True
        self.__restoreBreakpoints()
        self.__restoreWatchpoints()
        self.emit(SIGNAL('passiveDebugStarted'), fn, exc)
        
    def __passiveShutDown(self):
        """
        Private method to shut down a passive debug connection.
        """
        self.passiveClientExited = True
        self.shutdownServer()
        print unicode(self.trUtf8("Passive debug connection closed"))
        
    def __restoreBreakpoints(self):
        """
        Private method to restore the breakpoints after a restart.
        """
        if self.debugging:
            self.__addBreakPoints(QModelIndex(), 0, self.breakpointModel.rowCount()-1)
    
    def __restoreWatchpoints(self):
        """
        Private method to restore the watch expressions after a restart.
        """
        if self.debugging:
            self.__addWatchPoints(QModelIndex(), 0, self.watchpointModel.rowCount()-1)
    
    def getBreakPointModel(self):
        """
        Public slot to get a reference to the breakpoint model object.
        
        @return reference to the breakpoint model object (BreakPointModel)
        """
        return self.breakpointModel

    def getWatchPointModel(self):
        """
        Public slot to get a reference to the watch expression model object.
        
        @return reference to the watch expression model object (WatchPointModel)
        """
        return self.watchpointModel
    
    def isConnected(self):
        """
        Public method to test, if the debug server is connected to a backend.
        
        @return flag indicating a connection (boolean)
        """
        return self.debuggerInterface and self.debuggerInterface.isConnected()
