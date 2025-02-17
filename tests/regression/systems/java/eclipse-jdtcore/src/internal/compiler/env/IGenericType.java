package org.eclipse.jdt.internal.compiler.env;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.internal.compiler.*;
public interface IGenericType extends IDependent {
/**
 * Answer an int whose bits are set according the access constants
 * defined by the VM spec.
 */
int getModifiers();
/**
 * Answer whether the receiver contains the resolved binary form
 * or the unresolved source form of the type.
 */
boolean isBinaryType();
boolean isClass();
boolean isInterface(); }
