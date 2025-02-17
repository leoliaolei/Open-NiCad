# -*- coding: utf-8 -*-

# Copyright (c) 2004 - 2010 Detlev Offenbach <detlev@die-offenbachs.de>
#

"""
Module implementing a dialog for entering repeat counts.
"""

from PyQt4.QtCore import *
from PyQt4.QtGui import *

from Ui_QRegExpWizardRepeatDialog import Ui_QRegExpWizardRepeatDialog


class QRegExpWizardRepeatDialog(QDialog, Ui_QRegExpWizardRepeatDialog):
    """
    Class implementing a dialog for entering repeat counts.
    """
    def __init__(self,parent = None):
        """
        Constructor
        
        @param parent parent widget (QWidget)
        """
        QDialog.__init__(self,parent)
        self.setupUi(self)
        
        self.unlimitedButton.setChecked(True)
        
    @pyqtSignature("int")
    def on_lowerSpin_valueChanged(self, value):
        """
        Private slot to handle the lowerSpin valueChanged signal.
        
        @param value value of the spinbox (integer)
        """
        if self.upperSpin.value() < value:
            self.upperSpin.setValue(value)
        
    @pyqtSignature("int")
    def on_upperSpin_valueChanged(self, value):
        """
        Private slot to handle the upperSpin valueChanged signal.
        
        @param value value of the spinbox (integer)
        """
        if self.lowerSpin.value() > value:
            self.lowerSpin.setValue(value)
        
    def getRepeat(self):
        """
        Public method to retrieve the dialog's result.
        
        @return ready formatted repeat string (string)
        """
        if self.unlimitedButton.isChecked():
            return "*"
        elif self.minButton.isChecked():
            reps = self.minSpin.value()
            if reps == 1:
                return "+"
            else:
                return "{%d,}" % reps
        elif self.maxButton.isChecked():
            reps = self.maxSpin.value()
            if reps == 1:
                return "?"
            else:
                return "{,%d}" % reps
        elif self.exactButton.isChecked():
            reps = self.exactSpin.value()
            return "{%d}" % reps
        elif self.betweenButton.isChecked():
            repsMin = self.lowerSpin.value()
            repsMax = self.upperSpin.value()
            return "{%d,%d}" % (repsMin, repsMax)
