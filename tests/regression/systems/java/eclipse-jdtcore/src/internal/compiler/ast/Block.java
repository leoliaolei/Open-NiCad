package org.eclipse.jdt.internal.compiler.ast;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.internal.compiler.IAbstractSyntaxTreeVisitor;
import org.eclipse.jdt.internal.compiler.impl.*;
import org.eclipse.jdt.internal.compiler.codegen.*;
import org.eclipse.jdt.internal.compiler.flow.*;
import org.eclipse.jdt.internal.compiler.lookup.*;
public class Block extends Statement {
       public Statement[] statements;
       public int explicitDeclarations;
       // the number of explicit declaration , used to create scope
       public BlockScope scope;
       public static final Block None = new Block(0);
       public Block(int explicitDeclarations) {
             this.explicitDeclarations = explicitDeclarations; }
       public FlowInfo analyseCode(
             BlockScope currentScope,
             FlowContext flowContext,
             FlowInfo flowInfo) {
             // empty block
             if (statements == null)
                  return flowInfo;
             for (int i = 0, max = statements.length; i < max; i++) {
                  Statement stat;
                  if (!flowInfo.complainIfUnreachable((stat = statements[i]), scope)) {
                      flowInfo = stat.analyseCode(scope, flowContext, flowInfo); } }
             return flowInfo; }
       public static final Block EmptyWith(int sourceStart, int sourceEnd) {
             //return an empty block which position is s and e
             Block bk = new Block(0);
             bk.sourceStart = sourceStart;
             bk.sourceEnd = sourceEnd;
             return bk; }
       /**
        * Code generation for a block
        */
       public void generateCode(BlockScope currentScope, CodeStream codeStream) {
             if ((bits & IsReachableMASK) == 0) {
                  return; }
             int pc = codeStream.position;
             if (statements != null) {
                  for (int i = 0, max = statements.length; i < max; i++) {
                      statements[i].generateCode(scope, codeStream); }
             } // for local variable debug attributes
             if (scope != currentScope) { // was really associated with its own scope
                  codeStream.exitUserScope(scope); }
             codeStream.recordPositionsFrom(pc, this.sourceStart); }
       public boolean isEmptyBlock() {
             return statements == null; }
       public void resolve(BlockScope upperScope) {
             if (statements != null) {
                  scope =
                      explicitDeclarations == 0
                         ? upperScope
                         : new BlockScope(upperScope, explicitDeclarations);
                  int i = 0, length = statements.length;
                  while (i < length)
                      statements[i++].resolve(scope); } }
       public void resolveUsing(BlockScope givenScope) {
             // this optimized resolve(...) is sent only on none empty blocks
             scope = givenScope;
             if (statements != null) {
                  int i = 0, length = statements.length;
                  while (i < length)
                      statements[i++].resolve(scope); } }
       public String toString(int tab) {
             /* slow code */
             String s = tabString(tab);
             if (this.statements == null) {
                  s += "{\n"; //$NON-NLS-1$
                  s += tabString(tab);
                  s += "}"; //$NON-NLS-1$
                  return s; }
             //   s = s + (explicitDeclarations != 0
             //              ? " { // ---scope needed for "+String.valueOf(explicitDeclarations) +" locals------------ \n"
             //              : "{// ---NO scope needed------ \n") ;
             s += "{\n"; //$NON-NLS-1$
             s += this.toStringStatements(tab);
             s += tabString(tab);
             s += "}"; //$NON-NLS-1$
             return s; }
       public String toStringStatements(int tab) {
             if (this.statements == null)
                  return ""; //$NON-NLS-1$
             StringBuffer buffer = new StringBuffer();
             for (int i = 0; i < statements.length; i++) {
                  buffer.append(statements[i].toString(tab + 1));
                  if (statements[i] instanceof Block) {
                      buffer.append("\n"); //$NON-NLS-1$
                  } else {
                      buffer.append(";\n");  }//$NON-NLS-1$
             };
             return buffer.toString(); }
       public void traverse(
             IAbstractSyntaxTreeVisitor visitor,
             BlockScope blockScope) {
             if (visitor.visit(this, blockScope)) {
                  if (statements != null) {
                      int statementLength = statements.length;
                      for (int i = 0; i < statementLength; i++)
                         statements[i].traverse(visitor, scope); } }
             visitor.endVisit(this, blockScope); } }
