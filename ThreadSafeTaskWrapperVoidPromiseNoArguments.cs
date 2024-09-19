namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperVoidPromiseNoArguments : ThreadSafeTaskWrapperNoResultBase
    {
        private PromiseVoid _Promise;
        internal ThreadSafeTaskWrapperVoidPromiseNoArguments(PromiseVoid promise, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Promise.Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}