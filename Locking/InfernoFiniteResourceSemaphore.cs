using InfernoDispatcher.Core;
using InfernoDispatcher.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace InfernoDispatcher.Locking
{
    public class InfernoFiniteResourceSemaphore
    {
        private readonly object _lockObject = new object();
        private Queue<Tuple<Action, long>> _taskQueue = new Queue<Tuple<Action, long>>();
        private long _NTaken = 0;
        private int _NRunning = 0;
        private long _MaxN;

        public InfernoFiniteResourceSemaphore(long maxN) {
            if (maxN <= 0)
                throw new ArgumentException($"{nameof(maxN)} must be greater than zero");
            _MaxN = maxN;
        }
        public InfernoTaskWithResultBase<TResult> Enter<TResult>(
            long nResource, Func<TResult> callback)
        {
            var toReturn = new InactiveInfernoTaskWithResult<TResult>(callback);
            var run = () =>
            {
                toReturn.Run(null);
                OnTaskCompleted(nResource);
            };
            _Enter(run, nResource);
            return toReturn;
        }
        public InfernoTaskNoResultBase Enter(long nResource, Action callback)
        {
            var toReturn = new InactiveInfernoTaskNoResult(callback);
            var run = () =>
            {
                toReturn.Run(null);
                OnTaskCompleted(nResource);
            };
            _Enter(run, nResource);
            return toReturn;
        }
        public InfernoTaskWithResultBase<TResult> EnterCreateTask<TResult>(
            long nResource, Func<InfernoTaskWithResultBase<TResult>> createTask)
        {
            var toReturn = new InactiveInfernoTaskWithResult<TResult>(null);
            var run = () => RunTask(createTask, toReturn, nResource);
            _Enter(run, nResource);
            return toReturn;
        }
        public void EnterNoReturn(long nResource, Func<InfernoTask> createTask)
        {
            var run = () =>
            {
                RunTask(createTask, nResource);
            };
            _Enter(run, nResource);
        }
        public InfernoTaskNoResultBase Enter(long nResource, Func<InfernoTaskNoResultBase> createTask)
        {
            var toReturn = new InactiveInfernoTaskNoResult(null);
            var run = () =>
            {
                RunTask(createTask, toReturn, nResource);
            };
            _Enter(run, nResource);
            return toReturn;
        }
        private void _Enter(Action run, long nResource) {

            lock (_lockObject)
            {
                long potentialNewNTaken = _NTaken + nResource;
                if (potentialNewNTaken <= _MaxN || _NRunning <= 0)
                {
                    try
                    {
                        Dispatcher.Instance.Run(run);
                        _NTaken = potentialNewNTaken;
                        _NRunning++;
                    }
                    finally
                    {

                    }
                }
                else
                {
                    _taskQueue.Enqueue(new Tuple<Action, long>(run, nResource));
                }
            }
        }
        private void RunTask<TResult>(
            Func<InfernoTaskWithResultBase<TResult>> createTask,
            InactiveInfernoTaskWithResult<TResult> toReturn,
            long nResource)
        {
            try
            {
                var task = createTask();
                if (task == null) {
                    toReturn.Success(new object[] { null });
                    OnTaskCompleted(nResource);
                    return;
                }
                toReturn.AddFrom(task);
                task.ThenWhatever(doneState =>
                {
                    if (doneState.Exception != null)
                    {
                        toReturn.Fail(doneState.Exception);
                    }
                    else if (doneState.Canceled)
                    {
                        toReturn.Cancel();
                    }
                    else
                    {
                        toReturn.Success(doneState.Result);
                    }
                    OnTaskCompleted(nResource);
                });
            }
            catch (Exception ex)
            {
                toReturn.Fail(ex);
                OnTaskCompleted(nResource);
            }
        }
        private void RunTask(Func<InfernoTaskNoResultBase> createTask,
            InactiveInfernoTaskNoResult toReturn, long nResource)
        {
            try
            {
                var task = createTask();
                toReturn.AddFrom(task);
                task.ThenWhatever(doneState =>
                {
                    if (doneState.Exception != null)
                    {
                        toReturn.Fail(doneState.Exception);
                    }
                    else if (doneState.Canceled)
                    {
                        toReturn.Cancel();
                    }
                    else
                    {
                        toReturn.Success(doneState.Result);
                    }
                    OnTaskCompleted(nResource);
                });
            }
            catch (Exception ex)
            {
                toReturn.Fail(ex);
                OnTaskCompleted(nResource);
            }
        }
        private void RunTask(Func<InfernoTask> createTask, long nResource)
        {
            try
            {
                var task = createTask();
                task.ThenWhatever(() => OnTaskCompleted(nResource));
            }
            catch (Exception ex)
            {
                OnTaskCompleted(nResource);
            }
        }

        private void OnTaskCompleted(long nResource)
        {
            Action toRun;
            lock (_lockObject)
            {
                _NTaken -= nResource;
                if (_taskQueue.Count <= 0)
                {
                    _NRunning--;
                    return;
                }
                var nextTaskAndNResource = _taskQueue.Peek();
                long nextNResource = nextTaskAndNResource.Item2;
                long potentialNewNTaken = _NTaken + nextNResource;
                if (potentialNewNTaken > _MaxN && _NRunning > 1)
                {
                    _NRunning--;
                    return;
                }
                _taskQueue.Dequeue();
                _NTaken = potentialNewNTaken;
                toRun = nextTaskAndNResource.Item1;
            }
            toRun();
        }
    }
}
