// ConcurrentStack.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (c) 2008 Jérémie "Garuma" Laval
// Copyright (C) 2014 Xamarin Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Concurrent {

	[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
	[System.Diagnostics.DebuggerTypeProxy(typeof(CollectionDebuggerView<>))]
	public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IEnumerable<T>,
									  ICollection, IEnumerable {
		class Node {
			public T Value = default(T);
			public Node Next;
		}

		object head;
		Node Head {
			get {
				return (Node) head;
			}
		}

		int count;

		public ConcurrentStack() {
		}

		public ConcurrentStack(IEnumerable<T> collection) {
			if (collection == null)
				throw new ArgumentNullException("collection");

			foreach (T item in collection)
				Push(item);
		}

		bool IProducerConsumerCollection<T>.TryAdd(T elem) {
			Push(elem);
			return true;
		}

		public void Push(T item) {
			Node temp = new Node();
			temp.Value = item;
			do {
				temp.Next = Head;
			} while (Interlocked.CompareExchange(ref head, temp, temp.Next) != temp.Next);

			Interlocked.Increment(ref count);
		}

		public void PushRange(T[] items) {
			if (items == null)
				throw new ArgumentNullException("items");

			PushRange(items, 0, items.Length);
		}

		public void PushRange(T[] items, int startIndex, int count) {
			RangeArgumentsCheck(items, startIndex, count);

			Node insert = null;
			Node first = null;

			for (int i = startIndex; i < count; i++) {
				Node temp = new Node();
				temp.Value = items[i];
				temp.Next = insert;
				insert = temp;

				if (first == null)
					first = temp;
			}

			do {
				first.Next = Head;
			} while (Interlocked.CompareExchange(ref head, insert, first.Next) != first.Next);

			Interlocked.Add(ref this.count, count);
		}

		public bool TryPop(out T result) {
			Node temp;
			do {
				temp = Head;
				// The stak is empty
				if (temp == null) {
					result = default(T);
					return false;
				}
			} while (Interlocked.CompareExchange(ref head, temp.Next, temp) != temp);

			Interlocked.Decrement(ref count);

			result = temp.Value;

			return true;
		}

		public int TryPopRange(T[] items) {
			if (items == null)
				throw new ArgumentNullException("items");
			return TryPopRange(items, 0, items.Length);
		}

		public int TryPopRange(T[] items, int startIndex, int count) {
			RangeArgumentsCheck(items, startIndex, count);

			Node temp;
			Node end;

			do {
				temp = Head;
				if (temp == null)
					return 0;
				end = temp;
				for (int j = 0; j < count; j++) {
					end = end.Next;
					if (end == null)
						break;
				}
			} while (Interlocked.CompareExchange(ref head, end, temp) != temp);

			int i;
			for (i = startIndex; i < startIndex + count && temp != null; i++) {
				items[i] = temp.Value;
				end = temp;
				temp = temp.Next;
			}
			Interlocked.Add(ref this.count, -(i - startIndex));

			return i - startIndex;
		}

		public bool TryPeek(out T result) {
			Node myHead = Head;
			if (myHead == null) {
				result = default(T);
				return false;
			}
			result = myHead.Value;
			return true;
		}

		public void Clear() {
			// This is not satisfactory
			count = 0;
			head = null;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return (IEnumerator) InternalGetEnumerator();
		}

		public IEnumerator<T> GetEnumerator() {
			return InternalGetEnumerator();
		}

		IEnumerator<T> InternalGetEnumerator() {
			Node my_head = Head;
			if (my_head == null) {
				yield break;
			} else {
				do {
					yield return my_head.Value;
				} while ((my_head = my_head.Next) != null);
			}
		}

		void ICollection.CopyTo(Array array, int index) {
			ICollection ic = new List<T>(this);
			ic.CopyTo(array, index);
		}

		public void CopyTo(T[] array, int index) {
			if (array == null)
				throw new ArgumentNullException("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index");
			if (index > array.Length)
				throw new ArgumentException("index is equals or greather than array length", "index");

			IEnumerator<T> e = InternalGetEnumerator();
			int i = index;
			while (e.MoveNext()) {
				if (i == array.Length - index)
					throw new ArgumentException("The number of elememts in the collection exceeds the capacity of array", "array");
				array[i++] = e.Current;
			}
		}

		bool ICollection.IsSynchronized {
			get {
				return false;
			}
		}

		bool IProducerConsumerCollection<T>.TryTake(out T item) {
			return TryPop(out item);
		}

		object ICollection.SyncRoot {
			get {
				throw new NotSupportedException();
			}
		}

		public T[] ToArray() {
			return new List<T>(this).ToArray();
		}

		public int Count {
			get {
				return count;
			}
		}

		public bool IsEmpty {
			get {
				return count == 0;
			}
		}

		static void RangeArgumentsCheck(T[] items, int startIndex, int count) {
			if (items == null)
				throw new ArgumentNullException("items");
			if (startIndex < 0 || startIndex >= items.Length)
				throw new ArgumentOutOfRangeException("startIndex");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if (startIndex + count > items.Length)
				throw new ArgumentException("startIndex + count is greater than the length of items.");
		}
	}
}