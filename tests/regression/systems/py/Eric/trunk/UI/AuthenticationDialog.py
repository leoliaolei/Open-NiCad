# -*- coding: utf-8 -*-

# Copyright (c) 2008 - 2010 Detlev Offenbach <detlev@die-offenbachs.de>
#

"""
Module implementing the authentication dialog for the help browser.
"""

from PyQt4.QtGui import QDialog, QStyle

from Ui_AuthenticationDialog import Ui_AuthenticationDialog

class AuthenticationDialog(QDialog, Ui_AuthenticationDialog):
    """
    Class implementing the authentication dialog for the help browser.
    """
    def __init__(self, info, username, showSave = False, saveIt = False, parent = None):
        """
        Constructor
        
        @param info information to be shown (string or QString)
        @param username username as supplied by subversion (string or QString)
        @param showSave flag to indicate to show the save checkbox (boolean)
        @param saveIt flag indicating the value for the save checkbox (boolean)
        """
        QDialog.__init__(self, parent)
        self.setupUi(self)
        
        self.infoLabel.setText(info)
        self.usernameEdit.setText(username)
        self.saveCheckBox.setVisible(showSave)
        self.saveCheckBox.setChecked(saveIt)
        
        self.iconLabel.setText("")
        self.iconLabel.setPixmap(
            self.style().standardIcon(QStyle.SP_MessageBoxQuestion).pixmap(32, 32))
    
    def setData(self, username, password):
        """
        Public method to set the login data.
        
        @param username username (string or QString)
        @param password password (string or QString)
        """
        self.usernameEdit.setText(username)
        self.passwordEdit.setText(password)
    
    def getData(self):
        """
        Public method to retrieve the login data.
        
        @return tuple of two QString values (username, password)
        """
        return (self.usernameEdit.text(), self.passwordEdit.text())
    
    def shallSave(self):
        """
        Public method to check, if the login data shall be saved.
        
        @return flag indicating that the login data shall be saved (boolean)
        """
        return self.saveCheckBox.isChecked()
