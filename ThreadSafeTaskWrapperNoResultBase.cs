namespace InfernoDispatcher
{
    public abstract class ThreadSafeTaskWrapperNoResultBase : ThreadSafeTaskWrapper
    {
        internal ThreadSafeTaskWrapperNoResultBase(params ThreadSafeTaskWrapper[] froms) : base(froms)
        {

        }
        protected void Success()
        {
            base.Success(null);
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
    }
}