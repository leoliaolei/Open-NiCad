package org.eclipse.jdt.internal.core.jdom;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.core.resources.*;
import org.eclipse.jdt.core.Flags;
import org.eclipse.jdt.core.IJavaElement;
import org.eclipse.jdt.core.IPackageFragment;
import org.eclipse.jdt.core.jdom.*;
import org.eclipse.jdt.internal.core.Util;
import org.eclipse.jdt.internal.core.util.*;
/**
 * DOMCompilation unit provides an implementation of IDOMCompilationUnit.
 *
 * @see IDOMCompilationUnit
 * @see DOMNode
 */
class DOMCompilationUnit extends DOMNode implements IDOMCompilationUnit {
       /**
        * The comment and/or whitespace preceding the
        * first document fragment in this compilation
        * unit.
        */
       protected String fHeader;
/**
 * Creates a new empty COMPILATION_UNIT document fragment.
 */
DOMCompilationUnit() {
       fHeader="";  }//$NON-NLS-1$
/**
 * Creates a new COMPILATION_UNIT on the given range of the document.
 *
 * @param document - the document containing this node's original contents
 * @param sourceRange - a two element array of integers describing the
 *         entire inclusive source range of this node within its document.
 *        A compilation unit's source range is the entire document - i.e.
 *         the first integer is zero, and the second integer is the position
 *         of the last character in the document.
 */
DOMCompilationUnit(char[] document, int[] sourceRange) {
       super(document, sourceRange, null, new int[]{-1, -1});
       fHeader = "";  }//$NON-NLS-1$
/**
 * @see DOMNode#appendContents(CharArrayBuffer)
 */
protected void appendFragmentedContents(CharArrayBuffer buffer) {
       buffer.append(getHeader());
       appendContentsOfChildren(buffer); }
/**
 * @see IDOMNode#canHaveChildren()
 */
public boolean canHaveChildren() {
       return true; }
/**
 * @see IDOMCompilationUnit#getHeader()
 */
public String getHeader() {
       return fHeader; }
/**
 * @see IDOMNode#getJavaElement
 */
public IJavaElement getJavaElement(IJavaElement parent) throws IllegalArgumentException {
       if (parent.getElementType() == IJavaElement.PACKAGE_FRAGMENT) {
             return ((IPackageFragment)parent).getCompilationUnit(getName());
       } else {
             throw new IllegalArgumentException(Util.bind("element.illegalParent"));  } }//$NON-NLS-1$
/**
 * @see IDOMCompilationUnit#getName()
 */
public String getName() { 
       IDOMType topLevelType= null;
       IDOMType firstType= null;
       IDOMNode child= fFirstChild;
       while (child != null) {
             if (child.getNodeType() == IDOMNode.TYPE) {
                  IDOMType type= (IDOMType)child;
                  if (firstType == null) {
                      firstType= type; }
                  if (Flags.isPublic(type.getFlags())) {
                      topLevelType= type;
                      break; } }
             child= child.getNextNode(); }
       if (topLevelType == null) {
             topLevelType= firstType; }
       if (topLevelType != null) {
             return topLevelType.getName() + ".java";  //$NON-NLS-1$
       } else {
             return null; } }
/**
 * @see IDOMNode#getNodeType()
 */
public int getNodeType() {
       return IDOMNode.COMPILATION_UNIT; }
/**
 * Sets the header
 */
protected void initalizeHeader() {
       DOMNode child = (DOMNode)getFirstChild();
       if (child != null) {
             int childStart = child.getStartPosition();
             if (childStart > 1) {
                  setHeader(CharArrayOps.substring(fDocument, 0, childStart)); } } }
/**
 * @see IDOMNode#isAllowableChild(IDOMNode)
 */
public boolean isAllowableChild(IDOMNode node) {
       if (node != null) {
             int type= node.getNodeType();
             return type == IDOMNode.PACKAGE || type == IDOMNode.IMPORT || type == IDOMNode.TYPE; 
       } else {
             return false; } }
/**
 * @see DOMNode
 */
protected DOMNode newDOMNode() {
       return new DOMCompilationUnit(); }
/**
 * Normalizes this <code>DOMNode</code>'s source positions to include whitespace preceeding
 * the node on the line on which the node starts, and all whitespace after the node up to
 * the next node's start
 */
void normalize(ILineStartFinder finder) {
       super.normalize(finder);
       initalizeHeader(); }
/**
 * @see IDOMCompilationUnit@setHeader(String)
 */
public void setHeader(String comment) {
       fHeader= comment;
       fragment(); }
/**
 * @see IDOMCompilationUnit#setName(String)
 */
public void setName(String name) {}
/**
 * @see DOMNode#shareContents(DOMNode)
 */
protected void shareContents(DOMNode node) {
       super.shareContents(node);
       fHeader= ((DOMCompilationUnit)node).fHeader; }
/**
 * @see IDOMNode#toString()
 */
public String toString() {
       return "COMPILATION_UNIT: " + getName();  } }//$NON-NLS-1$
