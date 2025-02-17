package org.eclipse.jdt.internal.eval;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.internal.compiler.ClassFile;
import org.eclipse.jdt.internal.compiler.util.*;
import java.util.*;
/**
 * This contains information about the installed variables such as
 * their names, their types, the name of the class that defines them,
 * the binary of the class (to be passed to the name environment when
 * compiling the code snippet).
 */
public class VariablesInfo {
       GlobalVariable[] variables;;
       int variableCount;
       char[] packageName;
       char[] className;
       ClassFile[] classFiles;
/**
 * Creates a new variables info.
 * The name of the global variable class is the simple name of this class.
 * The package name can be null if the variables have been defined in the default package.
 */
public VariablesInfo(char[] packageName, char[] className, ClassFile[] classFiles, GlobalVariable[] variables, int variableCount) {
       this.packageName = packageName;
       this.className = className;
       this.classFiles = classFiles;
       this.variables = variables;
       this.variableCount = variableCount; }
/**
 * Returns the index of the given variable.
 * Returns -1 if not found.
 */
int indexOf(GlobalVariable var) {
       for (int i = 0; i < this.variableCount; i++) {
             if (var.equals(this.variables[i]))
                  return i;
       };
       return -1; }
/**
 * Returns the variable with the given name.
 * Returns null if not found.
 */
GlobalVariable varNamed(char[] name) {
       GlobalVariable[] vars = this.variables;
       for (int i = 0; i < this.variableCount; i++) {
             GlobalVariable var = vars[i];
             if (CharOperation.equals(name, var.name))
                  return var;
       };
       return null; } }
