package org.eclipse.jdt.core.jdom;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.internal.core.*;
/**
 * A factory used to create document fragment (DF) nodes. An 
 * <code>IDOMCompilationUnit</code> represents the root of a complete JDOM (that
 * is, a ".java" file). Other node types represent fragments of a compilation
 * unit.
 * <p>
 * The factory can be used to create empty DFs or it can create DFs from source
 * strings. All DFs created empty are assigned default values as required, such
 * that a call to <code>IDOMNode.getContents</code> will generate a valid source
 * string. See individual <code>create</code> methods for details on the default
 * values supplied. The factory does its best to recognize Java structures in
 * the source provided. If the factory is completely unable to recognize source
 * constructs, the factory method returns <code>null</code>.
 * </p>
 * <p>
 * Even if a DF is created successfully from source code, it does not guarantee
 * that the source code will compile error free. Similary, the contents of a DF
 * are not guaranteed to compile error free. However, syntactically correct 
 * source code is guaranteed to be recognized and successfully generate a DF.
 * Similarly, if all of the fragments of a JDOM are syntactically correct, the
 * contents of the entire document will be correct too.
 * </p>
 * <p>
 * The factory does not perform or provide any code formatting. Document 
 * fragments created on source strings must be pre-formatted. The JDOM attempts
 * to maintain the formatting of documents as best as possible. For this reason,
 * document fragments created for nodes that are to be strung together should 
 * end with a new-line character. Failing to do so will result in a document
 * that has elements strung together on the same line. This is especially
 * important if a source string ends with a // comment. In this case, it would
 * be syntactically incorrect to omit the new line character.
 * </p>
 * <p>
 * This interface is not intended to be implemented by clients.
 * </p>
 *
 * @see IDOMNode
 */
public interface IDOMFactory {
/**
 * Creates and return an empty JDOM. The initial content is an empty string.
 *
 * @return the new compilation unit
 */
public IDOMCompilationUnit createCompilationUnit();
/**
 * Creates a JDOM on the given source code. The syntax for the given source
 * code corresponds to CompilationUnit (JLS2 7.3).
 *
 * @param sourceCode the source code character array, or <code>null</code>
 * @param name the name of the compilation unit
 * @return the new compilation unit, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMCompilationUnit createCompilationUnit(char[] sourceCode, String name);
/**
 * Creates a JDOM on the given source code. The syntax for the given source
 * code corresponds to CompilationUnit (JLS2 7.3).
 *
 * @param sourceCode the source code string, or <code>null</code>
 * @param name the name of the compilation unit
 * @return the new compilation unit, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMCompilationUnit createCompilationUnit(String sourceCode, String name);
/**
 * Creates a default field document fragment. Initially the field will have
 * default protection, type <code>"Object"</code>, name <code>"aField"</code>,
 * no comment, and no initializer.
 *
 * @return the new field
 */
public IDOMField createField();
/**
 * Creates a field document fragment on the given source code. The given source
 * string corresponds to FieldDeclaration (JLS2 8.3) and ConstantDeclaration 
 * (JLS2 9.3) restricted to a single VariableDeclarator clause.
 *
 * @param sourceCode the source code
 * @return the new field, or <code>null</code> if unable to recognize
 *   the source code, if the source code is <code>null</code>, or when the source
 *   contains more than one VariableDeclarator clause
 */
public IDOMField createField(String sourceCode);
/**
 * Creates an empty import document fragment. Initially the import will have
 * name <code>"java.lang.*"</code>.
 *
 * @return the new import
 */
public IDOMImport createImport();
/**
 * Creates an import document fragment on the given source code. The syntax for
 * the given source string corresponds to ImportDeclaration (JLS2 7.5).
 *
 * @param sourceCode the source code
 * @return the new import, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMImport createImport(String sourceCode);
/**
 * Creates an empty initializer document fragment. Initially the initializer
 * will be static and have no body or comment.
 *
 * @return the new initializer
 */
public IDOMInitializer createInitializer();
/**
 * Creates an initializer document fragment from the given source code. The
 * syntax for the given source string corresponds to InstanceInitializer 
 * (JLS2 8.6) and StaticDeclaration (JLS2 8.7).
 *
 * @param sourceCode the source code
 * @return the new initializer, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMInitializer createInitializer(String sourceCode);
/**
 * Creates a default method document fragment. Initially the method
 * will have public visibility, return type <code>"void"</code>, be named 
 * <code>"newMethod"</code>, have no parameters, no comment, and an empty body.
 *
 * @return the new method
 */
public IDOMMethod createMethod();
/**
 * Creates a method document fragment on the given source code. The syntax for
 * the given source string corresponds to MethodDeclaration (JLS2 8.4),  
 * ConstructorDeclaration (JLS2 8.8), and AbstractMethodDeclaration (JLS2 9.4).
 *
 * @param sourceCode the source code
 * @return the new method, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMMethod createMethod(String sourceCode);
/**
 * Creates an empty package document fragment. Initially the package 
 * declaration will have no name.
 *
 * @return the new package
 */
public IDOMPackage createPackage();
/**
 * Creates a package document fragment on the given source code. The syntax for
 * the given source string corresponds to PackageDeclaration (JLS2 7.4).
 *
 * @param sourceCode the source code
 * @return the new package, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMPackage createPackage(String sourceCode);
/**
 * Creates a default type document fragment. Initially the type will be
 * a public class named <code>"AClass"</code>, with no members or comment.
 *
 * @return the new type
 */
public IDOMType createType();
/**
 * Creates a type document fragment on the given source code. The syntax for the
 * given source string corresponds to ClassDeclaration (JLS2 8.1) and 
 * InterfaceDeclaration (JLS2 9.1).
 *
 * @param sourceCode the source code
 * @return the new type, or <code>null</code> if unable to recognize
 *   the source code, or if the source code is <code>null</code>
 */
public IDOMType createType(String sourceCode); }
