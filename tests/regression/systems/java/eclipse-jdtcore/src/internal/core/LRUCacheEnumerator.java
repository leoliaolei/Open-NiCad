package org.eclipse.jdt.internal.core;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import java.util.Enumeration;
/**
 *     The <code>LRUCacheEnumerator</code> returns its elements in 
 *     the order they are found in the <code>LRUCache</code>, with the
 *     most recent elements first.
 *
 *     Once the enumerator is created, elements which are later added 
 *     to the cache are not returned by the enumerator.  However,
 *     elements returned from the enumerator could have been closed 
 *     by the cache.
 */
public class LRUCacheEnumerator implements Enumeration {
       /**
        *    Current element;
        */
       protected LRUEnumeratorElement fElementQueue;
       public static class LRUEnumeratorElement {
             /**
              *   Value returned by <code>nextElement()</code>;
              */
             public Object fValue;
             /**
              *   Next element
              */
             public LRUEnumeratorElement fNext;
             /**
              * Constructor
              */
             public LRUEnumeratorElement(Object value) {
                  fValue = value; } }
/**
 *     Creates a CacheEnumerator on the list of <code>LRUEnumeratorElements</code>.
 */
public LRUCacheEnumerator(LRUEnumeratorElement firstElement) {
       fElementQueue = firstElement; }
/**
 * Returns true if more elements exist.
 */
public boolean hasMoreElements() {
       return fElementQueue != null; }
/**
 * Returns the next element.
 */
public Object nextElement() {
       Object temp = fElementQueue.fValue;
       fElementQueue = fElementQueue.fNext;
       return temp; } }
