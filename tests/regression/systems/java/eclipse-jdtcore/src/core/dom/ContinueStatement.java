/*******************************************************************************
 * Copyright (c) 2001 International Business Machines Corp. and others.
 * All rights reserved. This program and the accompanying materials 
 * are made available under the terms of the Common Public License v0.5 
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/cpl-v05.html
 * 
 * Contributors:
 *     IBM Corporation - initial API and implementation
 ******************************************************************************/
package org.eclipse.jdt.core.dom;
/**
 * Continue statement AST node type.
 *
 * <pre>
 * ContinueStatement:
 *    <b>continue</b> [ Identifier ] <b>;</b>
 * </pre>
 * 
 * @since 2.0
 */
public class ContinueStatement extends Statement {
       /**
        * The label, or <code>null</code> if none; none by default.
        */
       private SimpleName optionalLabel = null;
       /**
        * Creates a new unparented continue statement node owned by the given 
        * AST. By default, the continue statement has no label.
        * <p>
        * N.B. This constructor is package-private.
        * </p>
        * 
        * @param ast the AST that is to own this node
        */
       ContinueStatement(AST ast) {
             super(ast); }
       /* (omit javadoc for this method)
        * Method declared on ASTNode.
        */
       ASTNode clone(AST target) {
             ContinueStatement result = new ContinueStatement(target);
             result.setLeadingComment(getLeadingComment());
             result.setLabel((SimpleName) ASTNode.copySubtree(target, getLabel()));
             return result; }
       /* (omit javadoc for this method)
        * Method declared on ASTNode.
        */
       boolean equalSubtrees(Object other) {
             if (!(other instanceof ContinueStatement)) {
                  return false; }
             ContinueStatement o = (ContinueStatement) other;
             return ASTNode.equalNodes(getLabel(), o.getLabel()); }
       /* (omit javadoc for this method)
        * Method declared on ASTNode.
        */
       void accept0(ASTVisitor visitor) {
             boolean visitChildren = visitor.visit(this);
             if (visitChildren) {
                  acceptChild(visitor, getLabel()); }
             visitor.endVisit(this); }
       /**
        * Returns the label of this continue statement, or <code>null</code> if
        * there is none.
        * 
        * @return the label, or <code>null</code> if there is none
        */ 
       public SimpleName getLabel() {
             return optionalLabel; }
       /**
        * Sets or clears the label of this continue statement.
        * 
        * @param label the label, or <code>null</code> if 
        *    there is none
        * @exception $precondition-violation:different-ast$
        * @exception $precondition-violation:not-unparented$
        */ 
       public void setLabel(SimpleName label) {
             // a ContinueStatement cannot occur inside a SimpleName - no cycles
             replaceChild(this.optionalLabel, label, false);
             this.optionalLabel = label; }
       /* (omit javadoc for this method)
        * Method declared on ASTNode.
        */
       int memSize() {
             return super.memSize() + 1 * 4; }
       /* (omit javadoc for this method)
        * Method declared on ASTNode.
        */
       int treeSize() {
             return
                  memSize()
                  + (optionalLabel == null ? 0 : getLabel().treeSize()); } }
