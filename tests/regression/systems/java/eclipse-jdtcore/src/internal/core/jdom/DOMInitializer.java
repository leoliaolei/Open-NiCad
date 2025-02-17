package org.eclipse.jdt.internal.core.jdom;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.core.IJavaElement;
import org.eclipse.jdt.core.IType;
import org.eclipse.jdt.core.jdom.IDOMInitializer;
import org.eclipse.jdt.core.jdom.IDOMNode;
import org.eclipse.jdt.internal.compiler.util.Util;
import org.eclipse.jdt.internal.core.JavaModelManager;
import org.eclipse.jdt.internal.core.util.CharArrayBuffer;
import org.eclipse.jdt.internal.core.util.CharArrayOps;
/**
 * DOMInitializer provides an implementation of IDOMInitializer.
 *
 * @see IDOMInitializer
 * @see DOMNode
 */
class DOMInitializer extends DOMMember implements IDOMInitializer {
       /**
        * The contents of the initializer's body when the
        * body has been altered from the contents in the
        * document, otherwise <code>null</code>.
        */
       protected String fBody;
       /**
        * The original inclusive source range of the
        * body in the document.
        */
       protected int[]  fBodyRange;
/**
 * Constructs an empty initializer node.
 */
DOMInitializer() { }
/**
 * Creates a new detailed INITIALIZER document fragment on the given range of the document.
 *
 * @param document - the document containing this node's original contents
 * @param sourceRange - a two element array of integers describing the
 *         entire inclusive source range of this node within its document.
 *        Contents start on and include the character at the first position.
 *         Contents end on and include the character at the last position.
 *         An array of -1's indicates this node's contents do not exist
 *         in the document.
 * @param commentRange - a two element array describing the comments that precede
 *         the member declaration. The first matches the start of this node's
 *         sourceRange, and the second is the new-line or first non-whitespace
 *         character following the last comment. If no comments are present,
 *         this array contains two -1's.
 * @param flags - an integer representing the modifiers for this member. The
 *         integer can be analyzed with org.eclipse.jdt.core.Flags
 * @param modifierRange - a two element array describing the location of
 *         modifiers for this member within its source range. The first integer
 *         is the first character of the first modifier for this member, and
 *         the second integer is the last whitespace character preceeding the
 *         next part of this member declaration. If there are no modifiers present
 *         in this node's source code (i.e. default protection), this array
 *         contains two -1's.
 * @param bodyStartPosition - the position of the open brace of the body
 *        of this initialzer.
 */
DOMInitializer(char[] document, int[] sourceRange, int[] commentRange, int flags, int[] modifierRange, int bodyStartPosition) {
       super(document, sourceRange, null, new int[]{-1, -1}, commentRange, flags, modifierRange);
       fBodyRange= new int[2];
       fBodyRange[0]= bodyStartPosition;
       fBodyRange[1]= sourceRange[1];
       setHasBody(true);
       setMask(MASK_DETAILED_SOURCE_INDEXES, true); }
/**
 * Creates a new simple INITIALIZER document fragment on the given range of the document.
 *
 * @param document - the document containing this node's original contents
 * @param sourceRange - a two element array of integers describing the
 *         entire inclusive source range of this node within its document.
 *        Contents start on and include the character at the first position.
 *         Contents end on and include the character at the last position.
 *         An array of -1's indicates this node's contents do not exist
 *         in the document.
 * @param flags - an integer representing the modifiers for this member. The
 *         integer can be analyzed with org.eclipse.jdt.core.Flags
 */
DOMInitializer(char[] document, int[] sourceRange, int flags) {
       this(document, sourceRange, new int[] {-1, -1}, flags, new int[] {-1, -1}, -1);
       setMask(MASK_DETAILED_SOURCE_INDEXES, false); }
/**
 * @see DOMMember#appendMemberBodyContents(CharArrayBuffer)
 */
protected void appendMemberBodyContents(CharArrayBuffer buffer) {
       if (hasBody()) {
             buffer
                  .append(getBody())
                  .append(fDocument, fBodyRange[1] + 1, fSourceRange[1] - fBodyRange[1]);
       } else {
             buffer.append("{}").append(Util.LINE_SEPARATOR);  } }//$NON-NLS-1$
/**
 * @see DOMMember#appendMemberDeclarationContents(CharArrayBuffer)
 */
protected void appendMemberDeclarationContents(CharArrayBuffer buffer) {}
/**
 * @see DOMNode#appendSimpleContents(CharArrayBuffer)
 */
protected void appendSimpleContents(CharArrayBuffer buffer) {
       // append eveything before my name
       buffer.append(fDocument, fSourceRange[0], fNameRange[0] - fSourceRange[0]);
       // append my name
       buffer.append(fName);
       // append everything after my name
       buffer.append(fDocument, fNameRange[1] + 1, fSourceRange[1] - fNameRange[1]); }
/**
 * @see IDOMInitializer#getBody()
 */
public String getBody() {
       becomeDetailed();
       if (hasBody()) {
             if (fBody != null) {
                  return fBody;
             } else {
                  return CharArrayOps.substring(fDocument, fBodyRange[0], fBodyRange[1] + 1 - fBodyRange[0]); }
       } else {
             return null; } }
/**
 * @see DOMNode#getDetailedNode()
 */
protected DOMNode getDetailedNode() {
       return (DOMNode)getFactory().createInitializer(getContents()); }
/**
 * @see IDOMNode#getJavaElement
 */
public IJavaElement getJavaElement(IJavaElement parent) throws IllegalArgumentException {
       if (parent.getElementType() == IJavaElement.TYPE) {
             int count = 1;
             IDOMNode previousNode = getPreviousNode();
             while (previousNode != null) {
                  if (previousNode instanceof DOMInitializer) {
                      count++; }
                  previousNode = previousNode.getPreviousNode(); }
             return ((IType) parent).getInitializer(count);
       } else {
             throw new IllegalArgumentException(Util.bind("element.illegalParent"));  } }//$NON-NLS-1$
/**
 * @see DOMMember#getMemberDeclarationStartPosition()
 */
protected int getMemberDeclarationStartPosition() {
       return fBodyRange[0]; }
/**
 * @see IDOMNode#getNodeType()
 */
public int getNodeType() {
       return IDOMNode.INITIALIZER; }
/**
 * @see IDOMNode#isSigantureEqual(IDOMNode).
 *
 * <p>This method always answers false since an initializer
 * does not have a signature.
 */
public boolean isSignatureEqual(IDOMNode node) {
       return false; }
/**
 * @see DOMNode
 */
protected DOMNode newDOMNode() {
       return new DOMInitializer(); }
/**
 * Offsets all the source indexes in this node by the given amount.
 */
protected void offset(int offset) {
       super.offset(offset);
       offsetRange(fBodyRange, offset); }
/**
 * @see IDOMInitializer#setBody(char[])
 */
public void setBody(String body) {
       becomeDetailed();
       fBody= body;
       setHasBody(body != null);
       fragment(); }
/**
 * @see IDOMInitializer#setName(String)
 */
public void setName(String name) {}
/**
 * @see DOMNode#shareContents(DOMNode)
 */
protected void shareContents(DOMNode node) {
       super.shareContents(node);
       DOMInitializer init= (DOMInitializer)node;
       fBody= init.fBody;
       fBodyRange= rangeCopy(init.fBodyRange); }
/**
 * @see IDOMNode#toString()
 */
public String toString() {
       return "INITIALIZER";  } }//$NON-NLS-1$
