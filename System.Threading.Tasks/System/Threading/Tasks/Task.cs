//
// Task.cs
//
// Authors:
//    Marek Safar  <marek.safar@gmail.com>
//    Jérémie Laval <jeremie dot laval at xamarin dot com>
//
// Copyright (c) 2008 Jérémie "Garuma" Laval
// Copyright 2011-2013 Xamarin Inc (http://www.xamarin.com).
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks {
	[System.Diagnostics.DebuggerDisplay("Id = {Id}, Status = {Status}")]
	[System.Diagnostics.DebuggerTypeProxy(typeof(TaskDebuggerView))]
	public class Task : IDisposable, IAsyncResult {
		// With this attribute each thread has its own value so that it's correct for our Schedule code
		// and for Parent property.
		[System.ThreadStatic]
		static Task current;

		// parent is the outer task in which this task is created
		Task parent;
		// A reference to a Task on which this continuation is attached to
		Task contAncestor;

		static int id = -1;
		static readonly TaskFactory defaultFactory = new TaskFactory();

		object childTasks; //CountdownEvent
		CountdownEvent ChildTasks {
			get {
				return (CountdownEvent) childTasks;
			}
		}

		int taskId;
		TaskCreationOptions creationOptions;

		internal TaskScheduler scheduler;

		object exSlot; //TaskExceptionSlot
		TaskExceptionSlot ExSlot {
			get {
				return (TaskExceptionSlot) exSlot;
			}
		}

		object wait_handle; //ManualResetEvent
		ManualResetEvent Wait_Handle {
			get {
				return (ManualResetEvent) wait_handle;
			}
		}

		static internal Task InternalCurrent {
			get {
				return current;
			}
		}

		TaskStatus status;

		TaskActionInvoker invoker;
		object state;
		internal AtomicBooleanValue executing;

		TaskCompletionQueue<IContinuation> continuations;

		CancellationToken token;
		CancellationTokenRegistration? cancellationRegistration;

		internal const TaskCreationOptions WorkerTaskNotSupportedOptions = TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;

		const TaskCreationOptions MaxTaskCreationOptions =
			TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler |
			TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent;

		public Task(Action action)
			: this(action, TaskCreationOptions.None) {

		}

		public Task(Action action, TaskCreationOptions creationOptions)
			: this(action, CancellationToken.None, creationOptions) {

		}

		public Task(Action action, CancellationToken cancellationToken)
			: this(action, cancellationToken, TaskCreationOptions.None) {

		}

		public Task(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(TaskActionInvoker.Create(action), null, cancellationToken, creationOptions, current) {
			if (action == null)
				throw new ArgumentNullException("action");
			if (creationOptions > MaxTaskCreationOptions || creationOptions < TaskCreationOptions.None)
				throw new ArgumentOutOfRangeException("creationOptions");
		}

		public Task(Action<object> action, object state)
			: this(action, state, TaskCreationOptions.None) {
		}

		public Task(Action<object> action, object state, TaskCreationOptions creationOptions)
			: this(action, state, CancellationToken.None, creationOptions) {
		}

		public Task(Action<object> action, object state, CancellationToken cancellationToken)
			: this(action, state, cancellationToken, TaskCreationOptions.None) {
		}

		public Task(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(TaskActionInvoker.Create(action), state, cancellationToken, creationOptions, current) {
			if (action == null)
				throw new ArgumentNullException("action");
			if (creationOptions > MaxTaskCreationOptions || creationOptions < TaskCreationOptions.None)
				throw new ArgumentOutOfRangeException("creationOptions");
		}

		internal Task(TaskActionInvoker invoker, object state, CancellationToken cancellationToken,
					   TaskCreationOptions creationOptions, Task parent = null, Task contAncestor = null, bool ignoreCancellation = false) {
			this.invoker = invoker;
			this.creationOptions = creationOptions;
			this.state = state;
			this.taskId = Interlocked.Increment(ref id);
			this.token = cancellationToken;
			this.parent = parent = parent == null ? current : parent;
			this.contAncestor = contAncestor;
			this.status = cancellationToken.IsCancellationRequested && !ignoreCancellation ? TaskStatus.Canceled : TaskStatus.Created;

			// Process creationOptions
			if (parent != null && HasFlag(creationOptions, TaskCreationOptions.AttachedToParent)
				&& !HasFlag(parent.CreationOptions, TaskCreationOptions.DenyChildAttach))
				parent.AddChild();

			if (token.CanBeCanceled && !ignoreCancellation)
				cancellationRegistration = token.Register(l => ((Task) l).CancelReal(), this);
		}

		static bool HasFlag(TaskCreationOptions opt, TaskCreationOptions member) {
			return (opt & member) == member;
		}

		#region Start
		public void Start() {
			Start(TaskScheduler.Current);
		}

		public void Start(TaskScheduler scheduler) {
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			if (status >= TaskStatus.WaitingToRun)
				throw new InvalidOperationException("The Task is not in a valid state to be started.");

			if (IsContinuation)
				throw new InvalidOperationException("Start may not be called on a continuation task");

			if (IsPromise)
				throw new InvalidOperationException("Start may not be called on a promise-style task");

			SetupScheduler(scheduler);
			Schedule(true);
		}

		internal void SetupScheduler(TaskScheduler scheduler) {
			this.scheduler = scheduler;
			Status = TaskStatus.WaitingForActivation;
		}

		public void RunSynchronously() {
			RunSynchronously(TaskScheduler.Current);
		}

		public void RunSynchronously(TaskScheduler scheduler) {
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			if (Status > TaskStatus.WaitingForActivation)
				throw new InvalidOperationException("The task is not in a valid state to be started");

			if (IsContinuation)
				throw new InvalidOperationException("RunSynchronously may not be called on a continuation task");

			if (IsPromise)
				throw new InvalidOperationException("RunSynchronously may not be called on a promise-style task");

			RunSynchronouslyCore(scheduler, true);
		}

		internal void RunSynchronouslyCore(TaskScheduler scheduler, bool throwException) {
			SetupScheduler(scheduler);
			Status = TaskStatus.WaitingToRun;

			try {
				if (scheduler.RunInline(this, false))
					return;
			} catch (Exception inner) {
				var ex = new TaskSchedulerException(inner);
				TrySetException(new AggregateException(ex), false, true);
				if (throwException)
					throw ex;

				return;
			}

			Schedule(throwException);
			WaitCore(Timeout.Infinite, CancellationToken.None, false);
		}
		#endregion

		#region ContinueWith
		public Task ContinueWith(Action<Task> continuationAction) {
			return ContinueWith(continuationAction, TaskContinuationOptions.None);
		}

		public Task ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions) {
			return ContinueWith(continuationAction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken) {
			return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler) {
			return ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			if (continuationAction == null)
				throw new ArgumentNullException("continuationAction");
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			return ContinueWith(TaskActionInvoker.Create(continuationAction), cancellationToken, continuationOptions, scheduler);
		}

		internal Task ContinueWith(TaskActionInvoker invoker, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			var lazyCancellation = false;
			lazyCancellation = (continuationOptions & TaskContinuationOptions.LazyCancellation) > 0;
			var continuation = new Task(invoker, null, cancellationToken, GetCreationOptions(continuationOptions), null, this, lazyCancellation);
			ContinueWithCore(continuation, continuationOptions, scheduler);

			return continuation;
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction) {
			return ContinueWith<TResult>(continuationFunction, TaskContinuationOptions.None);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions) {
			return ContinueWith<TResult>(continuationFunction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken) {
			return ContinueWith<TResult>(continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler) {
			return ContinueWith<TResult>(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken,
													TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			if (continuationFunction == null)
				throw new ArgumentNullException("continuationFunction");
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			return ContinueWith<TResult>(TaskActionInvoker.Create(continuationFunction), cancellationToken, continuationOptions, scheduler);
		}

		internal Task<TResult> ContinueWith<TResult>(TaskActionInvoker invoker, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			var lazyCancellation = false;
			lazyCancellation = (continuationOptions & TaskContinuationOptions.LazyCancellation) > 0;
			var continuation = new Task<TResult>(invoker, null, cancellationToken, GetCreationOptions(continuationOptions), parent, this, lazyCancellation);
			ContinueWithCore(continuation, continuationOptions, scheduler);

			return continuation;
		}

		internal void ContinueWithCore(Task continuation, TaskContinuationOptions options, TaskScheduler scheduler) {
			const TaskContinuationOptions wrongRan = TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.OnlyOnRanToCompletion;
			const TaskContinuationOptions wrongCanceled = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.OnlyOnCanceled;
			const TaskContinuationOptions wrongFaulted = TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.OnlyOnFaulted;

			if (((options & wrongRan) == wrongRan) || ((options & wrongCanceled) == wrongCanceled) || ((options & wrongFaulted) == wrongFaulted))
				throw new ArgumentException("continuationOptions", "Some options are mutually exclusive");

			// Already set the scheduler so that user can call Wait and that sort of stuff
			continuation.scheduler = scheduler;
			continuation.Status = TaskStatus.WaitingForActivation;

			ContinueWith(new TaskContinuation(continuation, options));
		}

		internal bool ContinueWith(IContinuation continuation, bool canExecuteInline = true) {
			if (IsCompleted) {
				if (!canExecuteInline)
					return false;

				continuation.Execute();
				return true;
			}

			continuations.Add(continuation);

			// Retry in case completion was achieved but event adding was too late
			if (IsCompleted && continuations.Remove(continuation)) {
				if (!canExecuteInline)
					return false;

				continuation.Execute();
			}

			return true;
		}

		internal void RemoveContinuation(IContinuation continuation) {
			continuations.Remove(continuation);
		}

		static internal TaskCreationOptions GetCreationOptions(TaskContinuationOptions kind) {
			TaskCreationOptions options = TaskCreationOptions.None;
			if ((kind & TaskContinuationOptions.AttachedToParent) > 0)
				options |= TaskCreationOptions.AttachedToParent;
			if ((kind & TaskContinuationOptions.PreferFairness) > 0)
				options |= TaskCreationOptions.PreferFairness;
			if ((kind & TaskContinuationOptions.LongRunning) > 0)
				options |= TaskCreationOptions.LongRunning;

			return options;
		}
		#endregion

		#region Internal and protected thingies
		internal void Schedule(bool throwException) {
			Status = TaskStatus.WaitingToRun;
			try {
				scheduler.QueueTask(this);
			} catch (Exception inner) {
				var ex = new TaskSchedulerException(inner);
				TrySetException(new AggregateException(ex), false, true);
				if (throwException)
					throw ex;
			}
		}

		void ThreadStart() {
			/* Allow scheduler to break fairness of deque ordering without
			 * breaking its semantic (the task can be executed twice but the
			 * second time it will return immediately
			 */
			if (!executing.TryRelaxedSet())
				return;

			// Disable CancellationToken direct cancellation
			if (cancellationRegistration != null) {
				cancellationRegistration.Value.Dispose();
				cancellationRegistration = null;
			}

			// If Task are ran inline on the same thread we might trash these values
			var saveCurrent = current;
			var saveScheduler = TaskScheduler.Current;

			current = this;
			TaskScheduler.Current = HasFlag(creationOptions, TaskCreationOptions.HideScheduler) ? TaskScheduler.Default : scheduler;

			if (!token.IsCancellationRequested) {

				status = TaskStatus.Running;

				try {
					InnerInvoke();
				} catch (System.OperationCanceledException2 oce) {
					if (token != CancellationToken.None && oce.CancellationToken == token)
						CancelReal();
					else
						HandleGenericException(oce);
				} catch (Exception e) {
					HandleGenericException(e);
				}
			} else {
				CancelReal();
			}

			if (saveCurrent != null)
				current = saveCurrent;
			if (saveScheduler != null)
				TaskScheduler.Current = saveScheduler;
			Finish();
		}

		internal bool TrySetCanceled() {
			if (IsCompleted)
				return false;

			if (!executing.TryRelaxedSet()) {
				var sw = new SpinWait();
				while (!IsCompleted)
					sw.SpinOnce();

				return false;
			}

			CancelReal();
			return true;
		}

		internal bool TrySetException(AggregateException aggregate, bool cancellation, bool observed) {
			if (IsCompleted)
				return false;

			if (!executing.TryRelaxedSet()) {
				var sw = new SpinWait();
				while (!IsCompleted)
					sw.SpinOnce();

				return false;
			}

			if (cancellation) {
				ExceptionSlot.Exception = aggregate;
				Thread.MemoryBarrier();

				CancelReal();
			} else {
				HandleGenericException(aggregate);
			}

			if (observed)
				ExSlot.Observed = true;

			return true;
		}

		internal bool TrySetExceptionObserved() {
			if (exSlot != null) {
				ExSlot.Observed = true;
				return true;
			}
			return false;
		}

		internal void Execute() {
			ThreadStart();
		}

		internal void AddChild() {
			if (childTasks == null)
				Interlocked.CompareExchange(ref childTasks, new CountdownEvent(1), null);

			ChildTasks.AddCount();
		}

		internal void ChildCompleted(AggregateException childEx) {
			if (childEx != null) {
				if (ExceptionSlot.ChildExceptions == null)
					Interlocked.CompareExchange(ref ExceptionSlot.childExceptions, new ConcurrentQueue<AggregateException>(), null);
				ExceptionSlot.ChildExceptions.Enqueue(childEx);
			}

			if (ChildTasks.Signal() && status == TaskStatus.WaitingForChildrenToComplete) {
				ProcessChildExceptions();
				Status = exSlot == null ? TaskStatus.RanToCompletion : TaskStatus.Faulted;
				ProcessCompleteDelegates();
				if (parent != null && NotifyParentOnFinish())
					parent = null;
			}
		}

		void InnerInvoke() {
			if (IsContinuation) {
				var ancestor = contAncestor;
				contAncestor = null;
				invoker.Invoke(ancestor, state, this);
			} else {
				invoker.Invoke(this, state, this);
			}
		}

		internal void Finish() {
			// If there was children created and they all finished, we set the countdown
			if (childTasks != null) {
				if (ChildTasks.Signal())
					ProcessChildExceptions(true);
			}

			// Don't override Canceled or Faulted
			if (status == TaskStatus.Running) {
				if (childTasks == null || ChildTasks.IsSet)
					Status = TaskStatus.RanToCompletion;
				else
					Status = TaskStatus.WaitingForChildrenToComplete;
			}

			if (wait_handle != null)
				Wait_Handle.Set();

			// Tell parent that we are finished
			if (parent != null && NotifyParentOnFinish()) {
				//
				// Break the reference back to the parent, otherwise any Tasks created from another Task's thread of 
				// execution will create an undesired linked-list that the GC cannot free. See bug #18398.
				//
				parent = null;
			}

			// Completions are already processed when task is canceled or faulted
			if (status == TaskStatus.RanToCompletion)
				ProcessCompleteDelegates();

			// Reset the current thingies
			if (current == this)
				current = null;
			if (TaskScheduler.Current == scheduler)
				TaskScheduler.Current = null;

			if (cancellationRegistration.HasValue)
				cancellationRegistration.Value.Dispose();
		}

		bool NotifyParentOnFinish() {
			if (!HasFlag(creationOptions, TaskCreationOptions.AttachedToParent))
				return true;
			if (HasFlag(parent.CreationOptions, TaskCreationOptions.DenyChildAttach))
				return true;
			if (status != TaskStatus.WaitingForChildrenToComplete)
				parent.ChildCompleted(Exception);

			return false;
		}

		void ProcessCompleteDelegates() {
			if (continuations.HasElements) {
				IContinuation continuation;
				while (continuations.TryGetNextCompletion(out continuation))
					continuation.Execute();
			}
		}

		void ProcessChildExceptions(bool isParent = false) {
			if (exSlot == null || ExSlot.ChildExceptions == null)
				return;

			if (ExceptionSlot.Exception == null)
				ExSlot.Exception = new AggregateException();

			AggregateException childEx;
			while (ExSlot.ChildExceptions.TryDequeue(out childEx))
				ExSlot.Exception.AddChildException(childEx);

			if (isParent) {
				Status = TaskStatus.Faulted;
				ProcessCompleteDelegates();
			}
		}
		#endregion

		#region Cancel and Wait related method

		internal void CancelReal(bool notifyParent = false) {
			Status = TaskStatus.Canceled;

			if (wait_handle != null)
				Wait_Handle.Set();

			ProcessCompleteDelegates();

			if (notifyParent && parent != null && NotifyParentOnFinish())
				parent = null;
		}

		void HandleGenericException(Exception e) {
			HandleGenericException(new AggregateException(e));
		}

		void HandleGenericException(AggregateException e) {
			ExceptionSlot.Exception = e;
			Thread.MemoryBarrier();
			Status = TaskStatus.Faulted;

			if (wait_handle != null)
				Wait_Handle.Set();

			ProcessCompleteDelegates();
		}

		internal bool WaitOnChildren() {
			if (Status == TaskStatus.WaitingForChildrenToComplete && childTasks != null) {
				ChildTasks.Wait();
				return true;
			}
			return false;
		}

		public void Wait() {
			Wait(Timeout.Infinite, CancellationToken.None);
		}

		public void Wait(CancellationToken cancellationToken) {
			Wait(Timeout.Infinite, cancellationToken);
		}

		public bool Wait(TimeSpan timeout) {
			return Wait(CheckTimeout(timeout), CancellationToken.None);
		}

		public bool Wait(int millisecondsTimeout) {
			return Wait(millisecondsTimeout, CancellationToken.None);
		}

		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken) {
			if (millisecondsTimeout < -1)
				throw new ArgumentOutOfRangeException("millisecondsTimeout");

			bool result = WaitCore(millisecondsTimeout, cancellationToken, true);

			if (IsCanceled)
				throw new AggregateException(new TaskCanceledException(this));

			var exception = Exception;
			if (exception != null)
				throw exception;

			return result;
		}

		internal bool WaitCore(int millisecondsTimeout, CancellationToken cancellationToken, bool runInline) {
			if (IsCompleted)
				return true;

			// If the task is ready to be run and we were supposed to wait on it indefinitely without cancellation, just run it
			if (runInline && Status == TaskStatus.WaitingToRun && millisecondsTimeout == Timeout.Infinite && scheduler != null && !cancellationToken.CanBeCanceled) {
				try {
					scheduler.RunInline(this, true);
				} catch (Exception e) {
					throw new TaskSchedulerException(e);
				}
			}

			bool result = true;

			if (!IsCompleted) {
				var continuation = new ManualResetContinuation();
				try {
					ContinueWith(continuation);
					result = continuation.Event.Wait(millisecondsTimeout, cancellationToken);
				} finally {
					if (!result)
						RemoveContinuation(continuation);
					continuation.Dispose();
				}
			}

			return result;
		}

		public static void WaitAll(params Task[] tasks) {
			WaitAll(tasks, Timeout.Infinite, CancellationToken.None);
		}

		public static void WaitAll(Task[] tasks, CancellationToken cancellationToken) {
			WaitAll(tasks, Timeout.Infinite, cancellationToken);
		}

		public static bool WaitAll(Task[] tasks, TimeSpan timeout) {
			return WaitAll(tasks, CheckTimeout(timeout), CancellationToken.None);
		}

		public static bool WaitAll(Task[] tasks, int millisecondsTimeout) {
			return WaitAll(tasks, millisecondsTimeout, CancellationToken.None);
		}

		public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			bool result = true;
			foreach (var t in tasks) {
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");

				result &= t.Status == TaskStatus.RanToCompletion;
			}

			if (!result) {
				var continuation = new CountdownContinuation(tasks.Length);
				try {
					foreach (var t in tasks)
						t.ContinueWith(continuation);

					result = continuation.Event.Wait(millisecondsTimeout, cancellationToken);
				} finally {
					List<Exception> exceptions = null;

					foreach (var t in tasks) {
						if (result) {
							if (t.Status == TaskStatus.RanToCompletion)
								continue;
							if (exceptions == null)
								exceptions = new List<Exception>();
							if (t.Exception != null)
								exceptions.AddRange(t.Exception.InnerExceptions);
							else
								exceptions.Add(new TaskCanceledException(t));
						} else {
							t.RemoveContinuation(continuation);
						}
					}

					continuation.Dispose();

					if (exceptions != null)
						throw new AggregateException(exceptions);
				}
			}

			return result;
		}

		public static int WaitAny(params Task[] tasks) {
			return WaitAny(tasks, Timeout.Infinite, CancellationToken.None);
		}

		public static int WaitAny(Task[] tasks, TimeSpan timeout) {
			return WaitAny(tasks, CheckTimeout(timeout));
		}

		public static int WaitAny(Task[] tasks, int millisecondsTimeout) {
			return WaitAny(tasks, millisecondsTimeout, CancellationToken.None);
		}

		public static int WaitAny(Task[] tasks, CancellationToken cancellationToken) {
			return WaitAny(tasks, Timeout.Infinite, cancellationToken);
		}

		public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");
			if (millisecondsTimeout < -1)
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			CheckForNullTasks(tasks);

			if (tasks.Length > 0) {
				var continuation = new ManualResetContinuation();
				bool result = false;
				try {
					for (int i = 0; i < tasks.Length; i++) {
						var t = tasks[i];
						if (t.IsCompleted)
							return i;
						t.ContinueWith(continuation);
					}

					if (!(result = continuation.Event.Wait(millisecondsTimeout, cancellationToken)))
						return -1;
				} finally {
					if (!result)
						foreach (var t in tasks)
							t.RemoveContinuation(continuation);
					continuation.Dispose();
				}
			}

			int firstFinished = -1;
			for (int i = 0; i < tasks.Length; i++) {
				var t = tasks[i];
				if (t.IsCompleted) {
					firstFinished = i;
					break;
				}
			}

			return firstFinished;
		}

		static int CheckTimeout(TimeSpan timeout) {
			try {
				return checked((int) timeout.TotalMilliseconds);
			} catch (System.OverflowException) {
				throw new ArgumentOutOfRangeException("timeout");
			}
		}

		static void CheckForNullTasks(Task[] tasks) {
			foreach (var t in tasks)
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");
		}
		#endregion

		#region Dispose
		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) {
			if (!IsCompleted)
				throw new InvalidOperationException("A task may only be disposed if it is in a completion state");

			// Set action to null so that the GC can collect the delegate and thus
			// any big object references that the user might have captured in a anonymous method
			if (disposing) {
				parent = null;
				invoker = null;
				state = null;
				if (cancellationRegistration != null)
					cancellationRegistration.Value.Dispose();
				if (wait_handle != null)
					((IDisposable) wait_handle).Dispose();
			}
		}
		#endregion

		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken,
								  TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			if (continuationAction == null)
				throw new ArgumentNullException("continuationAction");
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			Task continuation = new Task(TaskActionInvoker.Create(continuationAction),
										  state, cancellationToken,
										  GetCreationOptions(continuationOptions),
										  parent,
										  this);
			ContinueWithCore(continuation, continuationOptions, scheduler);

			return continuation;
		}

		public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) {
			return new ConfiguredTaskAwaitable(this, continueOnCapturedContext);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state) {
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken) {
			return ContinueWith(continuationAction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskContinuationOptions continuationOptions) {
			return ContinueWith(continuationAction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskScheduler scheduler) {
			return ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state) {
			return ContinueWith<TResult>(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskContinuationOptions continuationOptions) {
			return ContinueWith<TResult>(continuationFunction, state, CancellationToken.None, continuationOptions, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken) {
			return ContinueWith<TResult>(continuationFunction, state, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskScheduler scheduler) {
			return ContinueWith<TResult>(continuationFunction, state, CancellationToken.None, TaskContinuationOptions.None, scheduler);
		}

		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken,
													TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
			if (continuationFunction == null)
				throw new ArgumentNullException("continuationFunction");
			if (scheduler == null)
				throw new ArgumentNullException("scheduler");

			var t = new Task<TResult>(TaskActionInvoker.Create(continuationFunction),
									   state,
									   cancellationToken,
									   GetCreationOptions(continuationOptions),
									   parent,
									   this);

			ContinueWithCore(t, continuationOptions, scheduler);

			return t;
		}

		public static Task Delay(int millisecondsDelay) {
			return Delay(millisecondsDelay, CancellationToken.None);
		}

		public static Task Delay(TimeSpan delay) {
			return Delay(CheckTimeout(delay), CancellationToken.None);
		}

		public static Task Delay(TimeSpan delay, CancellationToken cancellationToken) {
			return Delay(CheckTimeout(delay), cancellationToken);
		}

		public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken) {
			if (millisecondsDelay < -1)
				throw new ArgumentOutOfRangeException("millisecondsDelay");

			if (cancellationToken.IsCancellationRequested)
				return TaskConstants.Canceled;

			var task = new Task(TaskActionInvoker.Empty, null, cancellationToken, TaskCreationOptions.None, null, null);
			task.SetupScheduler(TaskScheduler.Default);

			if (millisecondsDelay != Timeout.Infinite) {
				var timer = new Timer(delegate (object state) {
					var t = (Task) state;
					if (t.Status == TaskStatus.WaitingForActivation) {
						t.Status = TaskStatus.Running;
						t.Finish();
					}
				}, task, millisecondsDelay, -1);

				task.ContinueWith(new DisposeContinuation(timer));
			}

			return task;
		}

		public static Task<TResult> FromResult<TResult>(TResult result) {
			var tcs = new TaskCompletionSource<TResult>();
			tcs.SetResult(result);
			return tcs.Task;
		}

		public TaskAwaiter GetAwaiter() {
			return new TaskAwaiter(this);
		}

		public static Task Run(Action action) {
			return Run(action, CancellationToken.None);
		}

		public static Task Run(Action action, CancellationToken cancellationToken) {
			if (cancellationToken.IsCancellationRequested)
				return TaskConstants.Canceled;

			return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		public static Task Run(Func<Task> function) {
			return Run(function, CancellationToken.None);
		}

		public static Task Run(Func<Task> function, CancellationToken cancellationToken) {
			if (cancellationToken.IsCancellationRequested)
				return TaskConstants.Canceled;

			return TaskExtensionsImpl.Unwrap(Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
		}

		public static Task<TResult> Run<TResult>(Func<TResult> function) {
			return Run(function, CancellationToken.None);
		}

		public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken) {
			if (cancellationToken.IsCancellationRequested)
				return TaskConstants<TResult>.Canceled;

			return Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) {
			return Run(function, CancellationToken.None);
		}

		public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken) {
			if (cancellationToken.IsCancellationRequested)
				return TaskConstants<TResult>.Canceled;

			return TaskExtensionsImpl.Unwrap(Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
		}

		public static Task WhenAll(params Task[] tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAllCore(tasks);
		}

		public static Task WhenAll(IEnumerable<Task> tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			// Call ToList on input enumeration or we end up
			// enumerating it more than once
			return WhenAllCore(new List<Task>(tasks));
		}

		public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAllCore<TResult>(tasks);
		}

		public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			// Call ToList on input enumeration or we end up
			// enumerating it more than once
			return WhenAllCore<TResult>(new List<Task<TResult>>(tasks));
		}

		internal static Task<TResult[]> WhenAllCore<TResult>(IList<Task<TResult>> tasks) {
			if (tasks.Count == 0)
				return FromResult(new TResult[0]);

			foreach (var t in tasks) {
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");
			}

			var task = new Task<TResult[]>(TaskActionInvoker.Empty, null, CancellationToken.None, TaskCreationOptions.None, null, TaskConstants.Finished);
			task.SetupScheduler(TaskScheduler.Current);

			var continuation = new WhenAllContinuation<TResult>(task, tasks);
			foreach (var t in tasks)
				t.ContinueWith(continuation);

			return task;
		}

		public static Task<Task> WhenAny(params Task[] tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAnyCore(tasks);
		}

		public static Task<Task> WhenAny(IEnumerable<Task> tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAnyCore(new List<Task>(tasks));
		}

		public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAnyCore<TResult>(tasks);
		}

		public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) {
			if (tasks == null)
				throw new ArgumentNullException("tasks");

			return WhenAnyCore<TResult>(new List<Task<TResult>>(tasks));
		}

		static Task<Task<TResult>> WhenAnyCore<TResult>(IList<Task<TResult>> tasks) {
			if (tasks.Count == 0)
				throw new ArgumentException("The tasks argument contains no tasks", "tasks");

			int completed_index = -1;
			for (int i = 0; i < tasks.Count; ++i) {
				var t = tasks[i];
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");

				if (t.IsCompleted && completed_index < 0)
					completed_index = i;
			}

			var task = new Task<Task<TResult>>(TaskActionInvoker.Empty, null, CancellationToken.None, TaskCreationOptions.None, null, TaskConstants.Finished);

			if (completed_index > 0) {
				task.TrySetResult(tasks[completed_index]);
				return task;
			}

			task.SetupScheduler(TaskScheduler.Current);

			var continuation = new WhenAnyContinuation<Task<TResult>>(task, tasks);
			foreach (var t in tasks)
				t.ContinueWith(continuation);

			return task;
		}

		public static YieldAwaitable Yield() {
			return new YieldAwaitable();
		}

		internal static Task WhenAllCore(IList<Task> tasks) {
			bool all_completed = true;
			foreach (var t in tasks) {
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");

				all_completed &= t.Status == TaskStatus.RanToCompletion;
			}

			if (all_completed)
				return TaskConstants.Finished;

			var task = new Task(TaskActionInvoker.Empty, null, CancellationToken.None, TaskCreationOptions.None, null, TaskConstants.Finished);
			task.SetupScheduler(TaskScheduler.Current);

			var continuation = new WhenAllContinuation(task, tasks);
			foreach (var t in tasks)
				t.ContinueWith(continuation);

			return task;
		}

		internal static Task<Task> WhenAnyCore(IList<Task> tasks) {
			if (tasks.Count == 0)
				throw new ArgumentException("The tasks argument contains no tasks", "tasks");

			int completed_index = -1;
			for (int i = 0; i < tasks.Count; ++i) {
				var t = tasks[i];
				if (t == null)
					throw new ArgumentException("tasks", "the tasks argument contains a null element");

				if (t.IsCompleted && completed_index < 0)
					completed_index = i;
			}

			var task = new Task<Task>(TaskActionInvoker.Empty, null, CancellationToken.None, TaskCreationOptions.None, null, TaskConstants.Finished);

			if (completed_index > 0) {
				task.TrySetResult(tasks[completed_index]);
				return task;
			}

			task.SetupScheduler(TaskScheduler.Current);

			var continuation = new WhenAnyContinuation<Task>(task, tasks);
			foreach (var t in tasks)
				t.ContinueWith(continuation);

			return task;
		}
		#region Properties

		internal CancellationToken CancellationToken {
			get {
				return token;
			}
		}

		public static TaskFactory Factory {
			get {
				return defaultFactory;
			}
		}

		public static int? CurrentId {
			get {
				Task t = current;
				return t == null ? (int?) null : t.Id;
			}
		}

		public AggregateException Exception {
			get {
				if (exSlot == null || !IsFaulted)
					return null;
				ExSlot.Observed = true;
				return ExSlot.Exception;
			}
		}

		public bool IsCanceled {
			get {
				return status == TaskStatus.Canceled;
			}
		}

		public bool IsCompleted {
			get {
				return status >= TaskStatus.RanToCompletion;
			}
		}

		public bool IsFaulted {
			get {
				return status == TaskStatus.Faulted;
			}
		}

		public TaskCreationOptions CreationOptions {
			get {
				return creationOptions & MaxTaskCreationOptions;
			}
		}

		public TaskStatus Status {
			get {
				return status;
			}
			internal set {
				status = value;
				Thread.MemoryBarrier();
			}
		}

		internal TaskExceptionSlot ExceptionSlot {
			get {
				if (exSlot != null)
					return (TaskExceptionSlot) exSlot;
				Interlocked.CompareExchange(ref exSlot, new TaskExceptionSlot(this), null);
				return (TaskExceptionSlot) exSlot;
			}
		}

		public object AsyncState {
			get {
				return state;
			}
		}

		bool IAsyncResult.CompletedSynchronously {
			get {
				return false;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle {
			get {
				if (invoker == null)
					throw new ObjectDisposedException(GetType().ToString());

				if (wait_handle == null)
					Interlocked.CompareExchange(ref wait_handle, new ManualResetEvent(IsCompleted), null);

				return (ManualResetEvent) wait_handle;
			}
		}

		public int Id {
			get {
				return taskId;
			}
		}

		bool IsContinuation {
			get {
				return contAncestor != null;
			}
		}

		bool IsPromise {
			get {
				return invoker == TaskActionInvoker.Promise;
			}
		}

		internal Task ContinuationAncestor {
			get {
				return contAncestor;
			}
		}

		internal string DisplayActionMethod {
			get {
				Delegate d = invoker.Action;
				return d == null ? "<none>" : d.Method.ToString();
			}
		}

		#endregion
	}
}