//
// TpScheduler.cs
//
// Authors:
//    Jérémie Laval <jeremie dot laval at xamarin dot com>
//    Marek Safar  <marek.safar@gmail.com>
//
// Copyright (c) 2011 Jérémie "Garuma" Laval
// Copyright 2012 Xamarin Inc (http://www.xamarin.com).
//
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

using System.Collections.Generic;

namespace System.Threading.Tasks {
	sealed class TpScheduler : TaskScheduler {
		static readonly WaitCallback callback = TaskExecuterCallback;

		protected internal override void QueueTask(Task task) {
			if ((task.CreationOptions & TaskCreationOptions.LongRunning) != 0) {
				var thread = new Thread(l => ((Task) l).Execute()) {
					IsBackground = true
				};

				thread.Start(task);
				return;
			}

			ThreadPool.QueueUserWorkItem(callback, task);
		}

		static void TaskExecuterCallback(object obj) {
			Task task = (Task) obj;
			task.Execute();
		}

		protected override IEnumerable<Task> GetScheduledTasks() {
			throw new NotImplementedException();
		}


		protected internal override bool TryDequeue(Task task) {
			return false;
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
			if (taskWasPreviouslyQueued && !TryDequeue(task))
				return false;

			return TryExecuteTask(task);
		}
	}
}