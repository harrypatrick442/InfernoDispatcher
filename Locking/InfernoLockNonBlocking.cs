using InfernoDispatcher.Core;
using InfernoDispatcher.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace InfernoDispatcher.Locking
{
    public class InfernoLockNonBlocking
    {
        private readonly object _lockObject = new object();
        private Queue<Action> _taskQueue = new Queue<Action>();
        private bool _Taken = false;

        public InfernoTaskWithResultBase<TResult> Enter<TResult>(
            Func<TResult> callback)
        {
            var toReturn = new InactiveInfernoTaskWithResult<TResult>(callback);
            var run = () =>
            {
                toReturn.Run(null);
                OnTaskCompleted();
            };
            _Enter(run);
            return toReturn;
        }
        public InfernoTaskNoResultBase Enter(Action callback)
        {
            var toReturn = new InactiveInfernoTaskNoResult(callback);
            var run = () =>
            {
                toReturn.Run(null);
                OnTaskCompleted();
            };
            _Enter(run);
            return toReturn;
        }
        public InfernoTaskWithResultBase<TResult> EnterTask<TResult>(
            Func<InfernoTaskWithResultBase<TResult>> createTask)
        {
            var toReturn = new InactiveInfernoTaskWithResult<TResult>(null);
            var run = () => RunTask(createTask, toReturn);
            _Enter(run);
            return toReturn;
        }
        public void EnterNoReturn(Func<InfernoTask> createTask)
        {
            var run = () =>
            {
                RunTask(createTask);
            };
            _Enter(run);
        }
        public InfernoTaskNoResultBase Enter(Func<InfernoTaskNoResultBase> createTask)
        {
            var toReturn = new InactiveInfernoTaskNoResult(null);
            var run = () =>
            {
                RunTask(createTask, toReturn);
            };
            _Enter(run);
            return toReturn;
        }
        private void _Enter(Action run)
        {

            lock (_lockObject)
            {
                if (!_Taken)
                {
                    try
                    {
                        Dispatcher.Instance.Run(run);
                        _Taken = true;
                    }
                    finally
                    {

                    }
                }
                else
                {
                    _taskQueue.Enqueue(run);
                }
            }
        }
        private void RunTask<TResult>(
            Func<InfernoTaskWithResultBase<TResult>> createTask,
            InactiveInfernoTaskWithResult<TResult> toReturn)
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
                    OnTaskCompleted();
                });
            }
            catch (Exception ex)
            {
                toReturn.Fail(ex);
                OnTaskCompleted();
            }
        }
        private void RunTask(Func<InfernoTaskNoResultBase> createTask,
            InactiveInfernoTaskNoResult toReturn)
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
                    OnTaskCompleted();
                });
            }
            catch (Exception ex)
            {
                toReturn.Fail(ex);
                OnTaskCompleted();
            }
        }
        private void RunTask(Func<InfernoTask> createTask)
        {
            try
            {
                var task = createTask();
                task.ThenWhatever(() => OnTaskCompleted());
            }
            catch (Exception ex)
            {
                OnTaskCompleted();
            }
        }

        private void OnTaskCompleted()
        {
            Action toRun;
            lock (_lockObject)
            {
                if (_taskQueue.Count < 1)
                {
                    _Taken = false;
                    return;
                }
                toRun = _taskQueue.Dequeue();
            }
            toRun();
        }
    }
}
