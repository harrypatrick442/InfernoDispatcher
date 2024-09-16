namespace InfernoDispatcher
{
    internal class CountdownLatch
    {
        private object _LockObject = new object();
        private int _Count;
        public int Count { get { lock(_LockObject) return _Count; } }
        private EventWaitHandle _EventWaitHandle;

        public CountdownLatch(int count = 1)
        {
            _Count = count;
            _EventWaitHandle = new ManualResetEvent(count<1);
        }
        public void Increment()
        {
            lock (_LockObject)
            {
                if (_Count <= 0)
                    throw new Exception("This should never happen");
                _Count++;
            }
        }

        public void Signal()
        {
            // The last thread to signal also sets the event.
            lock (_LockObject)
            {
                _Count--;
                if (_Count <= 0)
                    _EventWaitHandle.Set();
            }
        }
        public void SignalToZero()
        {
            // The last thread to signal also sets the event.

            lock (_LockObject)
            {
                _Count = 0;
                _EventWaitHandle.Set();
            }
        }

        public void Wait()
        {
            _EventWaitHandle.WaitOne();
        }
        public bool Wait(int timeoutMilliseconds)
        {
            return _EventWaitHandle.WaitOne(timeoutMilliseconds);
        }
        public bool Wait(CancellationToken cancellationToken)
        {
            bool cancelled = false;
            using (CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(
                ()=> { cancelled = true; Signal(); }))
            {
                bool res = _EventWaitHandle.WaitOne();
                if (cancelled)
                {
                    return false;
                }
                return res;
            }
        }
        public bool Wait(int timeoutInterva, CancellationToken cancellationToken)
        {

            bool cancelled = false;
            using (CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(
                () => { cancelled = true; Signal(); }))
            {
                bool res = _EventWaitHandle.WaitOne(timeoutInterva);
                if (cancelled)
                {
                    return false;
                }
                return res;
            }
        }
    }
}
