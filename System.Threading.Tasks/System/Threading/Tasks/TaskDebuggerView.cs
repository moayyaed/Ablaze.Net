//
// TaskDebuggerView.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace System.Threading.Tasks {
	//
	// Custom debugger type proxy for tasks
	//
	sealed class TaskDebuggerView {
		readonly Task task;

		public TaskDebuggerView(Task task) {
			this.task = task;
		}

		public object AsyncState {
			get {
				return task.AsyncState;
			}
		}

		public TaskCreationOptions CreationOptions {
			get {
				return task.CreationOptions;
			}
		}

		public Exception Exception {
			get {
				return task.Exception;
			}
		}

		public int Id {
			get {
				return task.Id;
			}
		}

		public string Method {
			get {
				return task.DisplayActionMethod;
			}
		}

		public TaskStatus Status {
			get {
				return task.Status;
			}
		}
	}
}