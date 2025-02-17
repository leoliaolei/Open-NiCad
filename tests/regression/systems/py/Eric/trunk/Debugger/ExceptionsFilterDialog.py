# -*- coding: utf-8 -*-

# Copyright (c) 2002 - 2010 Detlev Offenbach <detlev@die-offenbachs.de>
#

"""
Module implementing the exceptions filter dialog.
"""

from PyQt4.QtCore import *
from PyQt4.QtGui import *

from Ui_ExceptionsFilterDialog import Ui_ExceptionsFilterDialog

class ExceptionsFilterDialog(QDialog, Ui_ExceptionsFilterDialog):
    """
    Class implementing the exceptions filter dialog.
    """
    def __init__(self, excList, ignore, parent=None):
        """
        Constructor
        
        @param excList list of exceptions to be edited (QStringList)
        @param ignore flag indicating the ignore exceptions mode (boolean)
        @param parent the parent widget (QWidget)
        """
        QDialog.__init__(self, parent)
        self.setupUi(self)
        self.setModal(True)
        
        self.exceptionList.addItems(excList)
        
        if ignore:
            self.setWindowTitle(self.trUtf8("Ignored Exceptions"))
            self.exceptionList.setToolTip(self.trUtf8("List of ignored exceptions"))
        
        self.okButton = self.buttonBox.button(QDialogButtonBox.Ok)
    
    @pyqtSignature("")
    def on_exceptionList_itemSelectionChanged(self):
        """
        Private slot to handle the change of the selection.
        """
        self.deleteButton.setEnabled(len(self.exceptionList.selectedItems()) > 0)
    
    @pyqtSignature("")
    def on_deleteButton_clicked(self):
        """
        Private slot to delete the currently selected exception of the listbox.
        """
        itm = self.exceptionList.takeItem(self.exceptionList.currentRow())
        del itm
    
    @pyqtSignature("")
    def on_deleteAllButton_clicked(self):
        """
        Private slot to delete all exceptions of the listbox.
        """
        while self.exceptionList.count() > 0:
            itm = self.exceptionList.takeItem(0)
            del itm

    @pyqtSignature("")
    def on_addButton_clicked(self):
        """
        Private slot to handle the Add button press.
        """
        exception = self.exceptionEdit.text()
        if not exception.isEmpty():
            self.exceptionList.addItem(exception)
            self.exceptionEdit.clear()
        
    def on_exceptionEdit_textChanged(self, txt):
        """
        Private slot to handle the textChanged signal of exceptionEdit.
        
        This slot sets the enabled status of the add button and sets the forms
        default button.
        
        @param txt the text entered into exceptionEdit (QString)
        """
        if txt.isEmpty():
            self.okButton.setDefault(True)
            self.addButton.setDefault(False)
            self.addButton.setEnabled(False)
        else:
            self.okButton.setDefault(False)
            self.addButton.setDefault(True)
            self.addButton.setEnabled(True)
        
    def getExceptionsList(self):
        """
        Public method to retrieve the list of exception types.
        
        @return list of exception types (list of strings)
        """
        excList = QStringList()
        for row in range(0, self.exceptionList.count()):
            itm = self.exceptionList.item(row)
            excList.append(itm.text())
        return excList
