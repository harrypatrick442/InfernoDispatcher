using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using InfernoDispatcher.Promises;
using InfernoDispatcher.Core;
using System.Collections.Generic;
namespace InfernoDispatcher.Tasks
{
    public abstract class InfernoTask : INotifyCompletion
    {
        protected bool _Cancelled;
        public bool Cancelled
        {
            get
            {
                lock (_LockObject)
                {
                    return _Cancelled;
                }
            }
        }
        internal object[]? _Result;
        internal object[]? Arguments { get; set; }
        protected readonly object _LockObject = new object();
        protected List<InfernoTask>? _Thens;
        protected List<InfernoTask>? _Catchs;
        protected List<InfernoTask>? _ThenWhatevers;
        protected bool _IsCompleted = false;
        public bool HasExceptions { get
            {
                lock (_LockObject) {
                    return _Exception != null;
                }
            } }
        public object? FirstResult { get
            {
                lock (_LockObject) {
                    return _Result!=null? _Result[0]: null;
                }
            } }
        public Exception? Exception { get { lock (_LockObject) { return _Exception; } } }
        public bool IsCompleted
        {
            get
            {
                lock (_LockObject)
                {
                    return _IsCompleted;
                }
            }
        }
        protected Exception? _Exception;
        internal CountdownLatch? _CountdownLatchWait;
        private InfernoTask[] _Froms;
        internal InfernoTask(params InfernoTask[] froms)
        {
            _Froms = froms;
        }
        public void AddFrom(InfernoTask from)
        {
            bool cancelled;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    var oldFroms = _Froms;
                    int length = oldFroms.Length;
                    _Froms = new InfernoTask[length + 1];
                    Array.Copy(oldFroms, _Froms, length);
                    _Froms[length] = from;
                    return;
                }
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                from.Cancel(this);
                return;
            }
        }
        public abstract void Run(object[]? arguments);
        public void Cancel()
        {
            Cancel(null);
        }
        protected void Cancel(InfernoTask? doingCancelIfNotNullConsiderOtherDependencies)
        {
            List<InfernoTask>? thens;
            List<InfernoTask>? thenWhatevers;
            InfernoTask[] froms;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                if (doingCancelIfNotNullConsiderOtherDependencies != null)
                {
                    bool hasOtherDependenciesNotCancelled =
                        _Thens == null/*just incase somehow*/? false : _Thens
                        .Where(t => !t.Equals(doingCancelIfNotNullConsiderOtherDependencies) && !t.Cancelled)
                        .Any();
                    if (hasOtherDependenciesNotCancelled)
                    {
                        return;
                    }
                }
                _IsCompleted = true;
                _Cancelled = true;
                thens = _Thens;
                _Thens = null;
                froms = _Froms;
                _Froms = null;
                thenWhatevers = _ThenWhatevers;
                _ThenWhatevers = null;
                _CountdownLatchWait?.Signal();
            }
            if (thenWhatevers != null)
            {
                var doneState = new object[] { new InfernoTaskDoneState(null, true, null) };
                foreach (InfernoTask thenWhatever in thenWhatevers)
                {
                    Dispatcher.Instance.Run(thenWhatever, doneState);
                }
            }
            if (froms != null)
            {
                foreach (InfernoTask from in froms)
                {
                    from.Cancel(this);
                }
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    then.Cancel();
                }
            }
        }
        internal TTask ExecuteOrScheduleTask<TTask>(TTask task, bool noRunCosAlreadyRun = false) where TTask : InfernoTask
        {
            Exception? exception;
            bool cancelled;
            object[]? runArguments;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    if (_Thens == null)
                    {
                        _Thens = new List<InfernoTask>(1) { task };
                    }
                    else
                    {
                        _Thens.Add(task);
                    }
                    return task;
                }
                runArguments = _Result;
                exception = _Exception;
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                task.Cancel();
                return task;
            }
            if (exception != null)
            {
                task.Fail(exception);
                return task;
            }
            if (!noRunCosAlreadyRun)
            {
                Dispatcher.Instance.Run(task, runArguments);
            }
            return task;
        }
        internal void Success(object[]? result)
        {
            List<InfernoTask>? thens;
            List<InfernoTask>? catchs;
            List<InfernoTask>? thenWhatevers;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                _IsCompleted = true;
                _Result = result;
                thens = _Thens;
                _Thens = null;
                catchs = _Catchs;
                _Catchs = null;
                thenWhatevers = _ThenWhatevers;
                _ThenWhatevers = null;
                _CountdownLatchWait?.Signal();
            }
            if (thenWhatevers != null)
            {
                var doneState = new object[] { new InfernoTaskDoneState(null, false, result) };
                foreach (var thenWhatever in thenWhatevers)
                {
                    Dispatcher.Instance.Run(thenWhatever, doneState);
                }
            }
            if (thens != null)
            {
                foreach (var then in thens)
                {
                    Dispatcher.Instance.Run(then, result);
                }
            }
            if (catchs != null)
            {
                foreach (var catcher in catchs)
                {
                    catcher.CompleteCatcherWithoutException(result);
                }
            }
        }
        public void Fail(Exception ex)
        {
            List<InfernoTask>? thens;
            List<InfernoTask>? catches;
            List<InfernoTask>? thenWhatevers;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                _Exception = ex;
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                catches = _Catchs;
                _Catchs = null;
                thenWhatevers = _ThenWhatevers;
                _ThenWhatevers = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    then.Fail(ex);
                }
            }
            if (catches != null)
            {
                foreach (var catcher in catches)
                {
                    catcher.Run(new object[] { ex });
                }
            }
            if (thenWhatevers != null)
            {
                var doneState = new object[] { new InfernoTaskDoneState(ex, false, null) };
                foreach (var thenWhatever in thenWhatevers)
                {
                    try
                    {
                        Dispatcher.Instance.Run(thenWhatever, doneState);
                    }
                    catch(Exception ex2)
                    {
                        thenWhatever.Fail(ex2);
                    }
                }
            }
        }
        internal void CheckNotAlreadyCompleted()
        {
            lock (_LockObject)
            {
                if (_IsCompleted) throw new InvalidOperationException($"Task already completed and {nameof(Run)} should not be getting called again");

            }
        }
        internal void ThrowException()
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_Exception!).Throw();
        }
        #region Then
        public InfernoTaskVoidPromiseNoArguments Then(PromiseVoid promise)
        {
            // Create a new InfernoTaskNoResult that runs the callback after this task completes
            InfernoTaskVoidPromiseNoArguments task = new InfernoTaskVoidPromiseNoArguments(
                promise, this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public InfernoTaskPromiseNoArgument<TNextResult> Then<TNextResult>(
            Promise<TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseNoArgument<TNextResult>(
                promise, this));
        }
        public InfernoTaskNoResult Then(Action callback)
        {
            // Create a new InfernoTaskNoResult that runs the callback after this task completes
            InfernoTaskNoResult task = new InfernoTaskNoResult(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public InfernoTaskNoResult Then(InfernoTaskNoResult task)
        {
            // Chain the existing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public InfernoTaskWithResult<TNextResult> Then<TNextResult>(Func<TNextResult> callback)
        {
            // Create a new InfernoTaskWithResult to chain a result-producing task
            InfernoTaskWithResult<TNextResult> task = new InfernoTaskWithResult<TNextResult>(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public InfernoTaskVoidPromiseReturnNoArgument Then(
            Func<PromiseVoid> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskVoidPromiseReturnNoArgument(
                promise, this));
        }

        public InfernoTaskWithResult<TNextResult> Then<TNextResult>(InfernoTaskWithResult<TNextResult> task)
        {
            // Chain the existing result-producing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task, noRunCosAlreadyRun: true);
            return task;
        }
        public InfernoTaskPromiseReturnNoArgument<TNextResult> Then<TNextResult>(
            Func<Promise<TNextResult>> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseReturnNoArgument<TNextResult>(
                promise, this));
        }
        #endregion
        public InfernoTaskWithResultBase<TNextResult> ThenExistingTask<TNextResult>(
            InactiveInfernoTaskWithResultArgument<TNextResult, TNextResult> task)
        {
            return _ThenExistingTask(task);
        }
        internal InfernoTaskWithResultBase<TNextResult> _ThenExistingTask<TNextResult>(
            InfernoTaskWithResultArgument<TNextResult, TNextResult> task)
        {
            // Chain the existing result-producing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task, noRunCosAlreadyRun: true);
            return task;
        }
        public InfernoTaskNoResultBase ThenCreateTask(Func<InfernoTask> callback)
        {
            InfernoTaskNoResult? toReturn = null;
            InfernoTaskNoResult task = new InfernoTaskNoResult(
                () =>
                {
                    try
                    {
                        var nextTask = callback();
                        nextTask.Then(toReturn!);
                    }
                    catch (Exception ex)
                    {
                        toReturn!.Fail(ex);
                    }
                }, this
            );
            toReturn = new InfernoTaskNoResult(() => { }, task);
            ExecuteOrScheduleTask(task);
            task.Catch(toReturn.Fail);
            return toReturn;
        }

        public InfernoTaskWithResult<TNextResult> ThenCreateTask<TNextResult>(
            Func<InfernoTaskWithResult<TNextResult>> callback)
        {
            // Chain a new result-producing task that is returned by the callback function
            TNextResult? result = default;
            InfernoTaskWithResult<TNextResult>? toReturn = null;
            InfernoTaskNoResult task = new InfernoTaskNoResult(
                () =>
                {
                    try
                    {
                        var childTask = callback();
                        childTask.Then(toReturn!);
                    }
                    catch (Exception ex)
                    {
                        toReturn!.Fail(ex);
                    }
                }, this
            );
            toReturn = new InfernoTaskWithResult<TNextResult>(() => result!, task);
            ExecuteOrScheduleTask(task);
            task.Catch(toReturn.Fail);
            return toReturn;
        }
        #region Join
        public InfernoTaskNoResult Join(InfernoTaskNoResultBase other, Action callback)
        {
            return Join(callback, new InfernoTaskNoResultBase[] { other });
        }

        public InfernoTaskNoResult Join(
            InfernoTaskNoResult other1,
            InfernoTaskNoResult other2,
            Action callback)
        {
            return Join(callback, other1, other2);
        }

        public InfernoTaskNoResult Join(
            InfernoTaskNoResult other1,
            InfernoTaskNoResult other2,
            InfernoTaskNoResult other3,
            Action callback)
        {
            return Join(callback, other1, other2, other3);
        }
        public InfernoTaskNoResult Join(
                    Action callback,
                    params InfernoTaskNoResultBase[] others)
        {
            var all = new InfernoTask[] { this }.Concat(others).ToArray();
            InfernoTaskNoResult taskToReturn = new InfernoTaskNoResult(callback, all);
            return _Join(taskToReturn, all);
        }
        public InfernoTaskWithResult<TResult> Join<TResult>(
                    Func<TResult>callback,
                    params InfernoTaskNoResult[] others)
        {
            var all = new InfernoTask[] { this }.Concat(others).ToArray();
            var taskToReturn = new InfernoTaskWithResult<TResult>(callback, all);
            return _Join(taskToReturn, all);
        }
        public InfernoTaskNoResult JoinNoResults(
                    params InfernoTask[] others)
        {
            var all = new InfernoTask[] { this }.Concat(others).ToArray();
            var taskToReturn = new InfernoTaskNoResult(() => { },
                all);
            return _Join(taskToReturn, all);
        }
        public static InfernoTaskNoResultBase Join(params InfernoTask[] tasks)
        {
            var taskToReturn = new InfernoTaskNoResult(() => { },
                tasks);
            return _Join(taskToReturn, tasks);
        }
        public static InfernoTaskNoResultBase Join(IEnumerable<InfernoTask> tasks)
        {
            var taskToReturn = new InfernoTaskNoResult(() => { },
                tasks.ToArray());
            return _Join(taskToReturn, tasks);
        }
        protected static TTaskToReturn _Join<TTaskToReturn>(
                    TTaskToReturn taskToReturn,
                    IEnumerable<InfernoTask> all) where TTaskToReturn:InfernoTask
        {
            object lockObject = new object();
            int doneCount = 0;
            int totalTasks = all.Count() + 1;
            Action checkIfDoneAndRunIfIs = () =>
            {
                lock (lockObject)
                {
                    doneCount++;
                    if (doneCount == totalTasks)
                    {
                        Dispatcher.Instance.Run(taskToReturn, null);
                    }
                }
            };
            foreach (var task in all)
            {
                task.Then(() => {
                    checkIfDoneAndRunIfIs();
                });
                task.Catch(ex=>
                taskToReturn.Fail(ex));
            }
            return taskToReturn;
        }
        #endregion
        #region Catch
        public InfernoTaskNoResult Catch(Action<Exception> callback)
        {
            InfernoTaskNoResult catcher = new InfernoTaskNoResult(() =>
            {
                Exception ex;
                lock (_LockObject)
                {
                    ex = _Exception!;
                }
                callback(ex);
            }, this);
            Exception? exception;
            bool cancelled;
            object[]? resultAsRunArguments;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    if (_Catchs == null)
                    {
                        _Catchs = new List<InfernoTask> { catcher };
                    }
                    else
                    {
                        _Catchs!.Add(catcher);
                    }
                    return catcher;
                }
                exception = _Exception;
                cancelled = _Cancelled;
                resultAsRunArguments = _Result;
            }
            if (cancelled)
            {
                catcher.Cancel();
                return catcher;
            }
            if (exception == null)
            {
                catcher.CompleteCatcherWithoutException(resultAsRunArguments);
                return catcher;
            }
            Dispatcher.Instance.Run(catcher, new object[] { exception });
            return catcher;
        }
        #endregion
        #region ThenWhatever
        /// <summary>
        /// Creates a task with no relation which runs even if this task fails or succeeeds.
        /// This is used by subsystems that schedule unrelated tasks in series
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public InfernoTask ThenWhatever(Action callback)
        {
            return ThenWhatever(new InfernoTaskWhatever((doneState)=>callback()));
        }
        public InfernoTask ThenWhatever(Action<InfernoTaskDoneState> callback)
        {
            return ThenWhatever(new InfernoTaskWhatever(callback));
        }
        public InfernoTask ThenWhatever(InfernoTaskWhatever task)
        {
            InfernoTaskDoneState doneState;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    if (_ThenWhatevers == null)
                    {
                        _ThenWhatevers = new List<InfernoTask> { task };
                    }
                    else
                    {
                        _ThenWhatevers!.Add(task);
                    }
                    return task;
                }
                doneState = new InfernoTaskDoneState(_Exception, _Cancelled, _Result); ;
            }
            Dispatcher.Instance.Run(task, new object[] { doneState });
            return task;
        }
        #endregion
        internal void CompleteCatcherWithoutException(object[]? arguments)
        {
            List<InfernoTask>? thens;
            List<InfernoTask>? thenWhatevers;
            lock (_LockObject)
            {
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                thenWhatevers = _ThenWhatevers;
                _ThenWhatevers = null;
                _CountdownLatchWait?.Signal();
            }
            if (thenWhatevers != null)
            {
                var doneState = new object[] { new InfernoTaskDoneState(null, false, arguments) };
                foreach (InfernoTask thenWhatever in thenWhatevers)
                {
                    Dispatcher.Instance.Run(thenWhatever, doneState);
                }
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    Dispatcher.Instance.Run(then, arguments);
                }
            }
        }
        #region Delay
        public InfernoTaskVoidPromiseNoArguments Delay(int millisecondsDelay, PromiseVoid promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskPromiseNoArgument<TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Promise<TNextResult> promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskNoResult Delay(int millisecondsDelay, Action callback)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(callback);
        }

        public InfernoTaskNoResult Delay(int millisecondsDelay, InfernoTaskNoResult task)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(task);
        }

        public InfernoTaskWithResult<TNextResult> Delay<TNextResult>(int millisecondsDelay, Func<TNextResult> callback)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(callback);
        }

        public InfernoTaskWithResult<TNextResult> Delay<TNextResult>(int millisecondsDelay, InfernoTaskWithResult<TNextResult> task)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(task);
        }
        public InfernoTaskPromiseReturnNoArgument<TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Func<Promise<TNextResult>> promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskVoidPromiseReturnNoArgument Delay<TNextResult>(
            int millisecondsDelay,
            Func<PromiseVoid> func)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(func);
        }
        #endregion
        #region Awaitable methods
        public void OnCompleted(Action continuation)
        {
            Then(continuation);
            Catch((ex) => continuation());
        }
        #endregion

        public void Wait()
        {
            lock (_LockObject)
            {
                if (_IsCompleted)
                {
                    if (_Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    if (_Exception != null)
                    {
                        ThrowException();
                        return;
                    }
                    return;
                }
                if (_CountdownLatchWait == null)
                {
                    _CountdownLatchWait = new CountdownLatch();
                }
            }
            _CountdownLatchWait.Wait();
            lock (_LockObject)
            {
                if (_Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (_Exception != null)
                {
                    ThrowException();
                    return;
                }
            }
        }
    }
}