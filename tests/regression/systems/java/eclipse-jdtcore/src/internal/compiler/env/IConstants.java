package org.eclipse.jdt.internal.compiler.env;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.internal.compiler.*;
/**
 * This interface defines constants for use by the builder / compiler interface.
 */
public interface IConstants {
       /*
        * Modifiers
        */
       int AccPublic = 0x0001;
       int AccPrivate = 0x0002;
       int AccProtected = 0x0004;
       int AccStatic = 0x0008;
       int AccFinal = 0x0010;
       int AccSynchronized = 0x0020;
       int AccVolatile = 0x0040;
       int AccTransient = 0x0080;
       int AccNative = 0x0100;
       int AccInterface = 0x0200;
       int AccAbstract = 0x0400;
       int AccStrictfp = 0x0800;
       /*
        * Other VM flags.
        */
       int AccSuper = 0x0020;
       /**
        * Extra flags for types and members.
        */
       int AccSynthetic = 0x20000;
       int AccDeprecated = 0x100000; }
