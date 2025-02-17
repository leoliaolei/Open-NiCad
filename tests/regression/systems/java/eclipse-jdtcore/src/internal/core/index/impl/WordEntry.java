package org.eclipse.jdt.internal.core.index.impl;
/*
 * (c) Copyright IBM Corp. 2000, 2001.
 * All Rights Reserved.
 */
import org.eclipse.jdt.core.*;
public class WordEntry {
       protected char[] fWord;
       protected int fNumRefs;
       protected int[] fRefs;
       public WordEntry() {
             this(new char[0]); }
       public WordEntry(char[] word) {
             fWord= word;
             fNumRefs= 0;
             fRefs= new int[10]; }
       /**
        * Adds a reference and records the change in footprint.
        */
       public int addRef(int fileNum) {
             if (fNumRefs > 0 && fRefs[fNumRefs - 1] == fileNum) {
                  return 0; }
             int footprintDelta= 0;
             if (fRefs.length == 0)
                  fRefs= new int[20];
             if (fNumRefs == fRefs.length) {
                  footprintDelta= fRefs.length * 4;
                  int[] newRefs= new int[fRefs.length * 2];
                  System.arraycopy(fRefs, 0, newRefs, 0, fRefs.length);
                  fRefs= newRefs; }
             fRefs[fNumRefs++]= fileNum;
             return footprintDelta; }
       /**
        * Adds a set of references and records the change in footprint.
        */
       public void addRefs(int[] refs) {
             int[] newRefs= new int[fNumRefs + refs.length];
             int pos1= 0;
             int pos2= 0;
             int posNew= 0;
             int compare;
             int r1= 0;
             int r2= 0;
             while (pos1 < fNumRefs || pos2 < refs.length) {
                  if (pos1 >= fNumRefs) {
                      r2= refs[pos2];
                      compare= -1;
                  } else if (pos2 >= refs.length) {
                      compare= 1;
                      r1= fRefs[pos1];
                  } else {
                      r1= fRefs[pos1];
                      r2= refs[pos2];
                      compare= r2 - r1; }
                  if (compare > 0) {
                      newRefs[posNew]= r1;
                      posNew++;
                      pos1++;
                  } else {
                      if (r2 != 0) {
                         newRefs[posNew]= r2;
                         posNew++; }
                      pos2++; } }
             fRefs= newRefs;
             fNumRefs= posNew;
             /*for (int i = 0; i < refs.length; i++)
             addRef(refs[i]);
             int[] newRefs = new int[fNumRefs];
             System.arraycopy(fRefs, 0, newRefs, 0, fNumRefs);
             fRefs = newRefs;
             Util.sort(fRefs);*/ }
       /**
        * Returns the size of the wordEntry
        */
       public int footprint() {
             return 8 + (3 * 4) + (8 + fWord.length * 2) + (8 + fRefs.length * 4); }
       /**
        * Returns the number of references, e.g. the number of files this word appears in.
        */
       public int getNumRefs() {
             return fNumRefs; }
       /**
        * returns the file number in the i position in the list of references.
        */
       public int getRef(int i) {
             if (i >= fNumRefs)
                  throw new IndexOutOfBoundsException();
             return fRefs[i]; }
       /**
        * Returns the references of the wordEntry (the number of the files it appears in).
        */
       public int[] getRefs() {
             int[] result= new int[fNumRefs];
             System.arraycopy(fRefs, 0, result, 0, fNumRefs);
             return result; }
       /**
        * returns the word of the wordEntry.
        */
       public char[] getWord() {
             return fWord; }
       /**
        * Changes the references of the wordEntry to match the mapping. For example,<br>
        * if the current references are [1 3 4]<br>
        * and mapping is [1 2 3 4 5]<br>
        * in references 1 becomes mapping[1] = 2, 3->4, and 4->5<br>
        * => references = [2 4 5].<br>
        */
       public void mapRefs(int[] mappings) {
             int position= 0;
             for (int i= 0; i < fNumRefs; i++) {
                  int map= mappings[fRefs[i]];
                  if (map != -1 && map != 0) {
                      fRefs[position]= map;
                      position++; } }
             fNumRefs= position;
             //to be changed!
             System.arraycopy(fRefs, 0, (fRefs= new int[fNumRefs]), 0, fNumRefs);
             Util.sort(fRefs); }
       /**
        * Clears the wordEntry.
        */
       public void reset(char[] word) {
             for (int i= fNumRefs; i-- > 0;) {
                  fRefs[i]= 0; }
             fNumRefs= 0;
             fWord= word; }
       public String toString() {
             return new String(fWord); } }
