using InfernoDispatcher.Core;

namespace InfernoDispatcher.Tasks
{
    public abstract class InfernoTaskNoResultBase : InfernoTask
    {
        internal InfernoTaskNoResultBase(params InfernoTask[] froms) : base(froms)
        {

        }
        public void Success()
        {
            Success(null);
        }
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
        #region Awaitable methods
        public virtual void GetResult()
        {
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
        #endregion
    }
}