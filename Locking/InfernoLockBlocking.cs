using InfernoDispatcher.Core;
using InfernoDispatcher.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace InfernoDispatcher.Locking
{
    public class InfernoLockBlocking
    {
        private readonly object _lockObject = new object();
        private Queue<Action> _taskQueue = new Queue<Action>();
        private bool _isLocked = false;

        public InfernoTaskWithResultBase<TResult> Enter<TResult>(Func<InfernoTaskWithResultBase<TResult>> createTask)
        {
            var toReturn = new InactiveInfernoTaskWithResult<TResult>(null);

            // Block until the lock is available
            lock (_lockObject)
            {
                while (_isLocked)
                {
                    Monitor.Wait(_lockObject); // Wait until the lock is released
                }

                // Lock acquired, set isLocked to true
                _isLocked = true;
                RunTask(createTask, toReturn);
            }

            return toReturn;
        }
        public InfernoTaskNoResultBase Enter(Func<InfernoTask> createTask)
        {
            var toReturn = new InactiveInfernoTaskNoResult(null);

            // Block until the lock is available
            lock (_lockObject)
            {
                while (_isLocked)
                {
                    Monitor.Wait(_lockObject); // Wait until the lock is released
                }

                // Lock acquired, set isLocked to true
                _isLocked = true;
                RunTask(createTask, toReturn);
            }

            return toReturn;
        }

        private void RunTask<TResult>(Func<InfernoTaskWithResultBase<TResult>> createTask, InactiveInfernoTaskWithResult<TResult> toReturn)
        {
            try
            {
                var task = createTask();
                toReturn.AddFrom(task);
                task.Then(result =>
                {
                    // If the task succeeds, call Success on the return task
                    toReturn.Success(result);
                    OnTaskCompleted();
                })
                .Catch(ex =>
                {
                    // If the task fails, call Failed on the return task
                    toReturn.Fail(ex);
                    OnTaskCompleted();
                });
            }
            catch (Exception ex)
            {
                // If creating the task throws an exception, immediately fail
                toReturn.Fail(ex);
                OnTaskCompleted();
            }
        }

        private void RunTask(Func<InfernoTask> createTask, InactiveInfernoTaskNoResult toReturn)
        {
            try
            {
                var task = createTask();
                toReturn.AddFrom(task);
                task.Then(()=>
                {
                    // If the task succeeds, call Success on the return task
                    toReturn.Success();
                    OnTaskCompleted();
                })
                .Catch(ex =>
                {
                    // If the task fails, call Failed on the return task
                    toReturn.Fail(ex);
                    OnTaskCompleted();
                });
            }
            catch (Exception ex)
            {
                // If creating the task throws an exception, immediately fail
                toReturn.Fail(ex);
                OnTaskCompleted();
            }
        }

        private void OnTaskCompleted()
        {
            lock (_lockObject)
            {
                if (_taskQueue.Count > 0)
                {
                    // Run the next task in the queue
                    var nextTask = _taskQueue.Dequeue();
                    nextTask();
                }
                else
                {
                    // No more tasks in the queue, unlock and notify waiting threads
                    _isLocked = false;
                    Monitor.PulseAll(_lockObject); // Wake up any waiting threads
                }
            }
        }
    }
}
