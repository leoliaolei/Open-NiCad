#region CVS Version Header
/*
 * $Id: Relations.cs,v 1.2 2005/01/18 17:36:43 carnage4life Exp $
 * Last modified by $Author: carnage4life $
 * Last modified at $Date: 2005/01/18 17:36:43 $
 * $Revision: 1.2 $
 */
#endregion

//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------

using System;
using System.Collections;

using NewsComponents.RelationCosmos;

namespace NewsComponents.Collections {
	public 
		class Relations : IDictionary, ICollection, IEnumerable, ICloneable {
		protected Hashtable innerHash;
		
		#region "Constructors"
		public  Relations() {
			innerHash = new Hashtable(StringComparer.Comparer,StringComparer.Comparer); 
		}
		public Relations(Relations original) {
			innerHash = new Hashtable (original.innerHash, StringComparer.Comparer, StringComparer.Comparer);
		}
		public Relations(IDictionary dictionary) {
			innerHash = new Hashtable (dictionary, StringComparer.Comparer, StringComparer.Comparer);
		}

		public Relations(int capacity) {
			innerHash = new Hashtable(capacity,StringComparer.Comparer,StringComparer.Comparer);
		}

		public Relations(IDictionary dictionary, float loadFactor) {
			innerHash = new Hashtable(dictionary, loadFactor,StringComparer.Comparer,StringComparer.Comparer);
		}

		public Relations(IHashCodeProvider codeProvider, IComparer comparer) {
			innerHash = new Hashtable (codeProvider, comparer);
		}

		public Relations(int capacity, int loadFactor) {
			innerHash = new Hashtable(capacity, loadFactor,StringComparer.Comparer,StringComparer.Comparer);
		}

		public Relations(IDictionary dictionary, IHashCodeProvider codeProvider, IComparer comparer) {
			innerHash = new Hashtable (dictionary, codeProvider, comparer);
		}
		
		public Relations(int capacity, IHashCodeProvider codeProvider, IComparer comparer) {
			innerHash = new Hashtable (capacity, codeProvider, comparer);
		}

		public Relations(IDictionary dictionary, float loadFactor, IHashCodeProvider codeProvider, IComparer comparer) {
			innerHash = new Hashtable (dictionary, loadFactor, codeProvider, comparer);
		}

		public Relations(int capacity, float loadFactor, IHashCodeProvider codeProvider, IComparer comparer) {
			innerHash = new Hashtable (capacity, loadFactor, codeProvider, comparer);
		}

		
		#endregion

		#region Implementation of IDictionary
		public RelationsEnumerator GetEnumerator() {
			return new RelationsEnumerator(this);
		}
        
		System.Collections.IDictionaryEnumerator IDictionary.GetEnumerator() {
			return new RelationsEnumerator(this);
		}
		
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void Remove(string key) {
			innerHash.Remove (key);
		}
		void IDictionary.Remove(object key) {
			Remove ((string)key);
		}

		public bool Contains(string key) {
			return innerHash.Contains(key);
		}
		bool IDictionary.Contains(object key) {
			return Contains((string)key);
		}

		public void Clear() {
			innerHash.Clear();		
		}

		public void Add(string key, RelationBase value) {
			innerHash.Add (key, value);
		}
		void IDictionary.Add(object key, object value) {
			Add ((string)key, (RelationBase)value);
		}

		public bool IsReadOnly {
			get {
				return innerHash.IsReadOnly;
			}
		}

		public RelationBase this[string key] {
			get {
				return (RelationBase) innerHash[key];
			}
			set {
				innerHash[key] = value;
			}
		}
		object IDictionary.this[object key] {
			get {
				return this[(string)key];
			}
			set {
				this[(string)key] = (RelationBase)value;
			}
		}
        
		public System.Collections.ICollection Values {
			get {
				return innerHash.Values;
			}
		}

		public System.Collections.ICollection Keys {
			get {
				return innerHash.Keys;
			}
		}

		public bool IsFixedSize {
			get {
				return innerHash.IsFixedSize;
			}
		}
		#endregion

		#region Implementation of ICollection
		public void CopyTo(System.Array array, int index) {
			innerHash.CopyTo (array, index);
		}

		public bool IsSynchronized {
			get {
				return innerHash.IsSynchronized;
			}
		}

		public int Count {
			get {
				return innerHash.Count;
			}
		}

		public object SyncRoot {
			get {
				return innerHash.SyncRoot;
			}
		}
		#endregion

		#region Implementation of ICloneable
		public Relations Clone() {
			Relations clone = new Relations();
			clone.innerHash = (Hashtable) innerHash.Clone();
			
			return clone;
		}
		object ICloneable.Clone() {
			return Clone();
		}
		#endregion
		
		#region "HashTable Methods"
		public bool ContainsKey (string key) {
			return innerHash.ContainsKey(key);
		}
		public bool ContainsValue (RelationBase value) {
			return innerHash.ContainsValue(value);
		}
		public static Relations Synchronized(Relations nonSync) {
			Relations sync = new Relations();
			sync.innerHash = Hashtable.Synchronized(nonSync.innerHash);

			return sync;
		}
		#endregion

		internal Hashtable InnerHash {
			get {
				return innerHash;
			}
		}
	}
	
	public class RelationsEnumerator : IDictionaryEnumerator {
		private IDictionaryEnumerator innerEnumerator;
			
		internal RelationsEnumerator (Relations enumerable) {
			innerEnumerator = enumerable.InnerHash.GetEnumerator();
		}

		#region Implementation of IDictionaryEnumerator
		public string Key {
			get {
				return (string)innerEnumerator.Key;
			}
		}
		object IDictionaryEnumerator.Key {
			get {
				return Key;
			}
		}


		public RelationBase Value {
			get {
				return (RelationBase)innerEnumerator.Value;
			}
		}
		object IDictionaryEnumerator.Value {
			get {
				return Value;
			}
		}

		public System.Collections.DictionaryEntry Entry {
			get {
				return innerEnumerator.Entry;
			}
		}

		#endregion

		#region Implementation of IEnumerator
		public void Reset() {
			innerEnumerator.Reset();
		}

		public bool MoveNext() {
			return innerEnumerator.MoveNext();
		}

		public object Current {
			get {
				return innerEnumerator.Current;
			}
		}
		#endregion
	}

	public class StringComparer: IHashCodeProvider, IComparer{	

		public static StringComparer Comparer = new StringComparer(); 
	
		public int Compare(object s1, object s2){
		
			if(Object.ReferenceEquals(s1, s2)){
				return 0; 
			}else {
				return -1; 
			}			
		}

		public int GetHashCode(object s){			
			return s.GetHashCode(); 
		}


	}

}
