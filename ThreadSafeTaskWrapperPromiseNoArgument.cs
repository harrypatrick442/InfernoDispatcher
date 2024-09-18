namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperPromiseNoArgument<TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Promise<TThisResult> _Promise;
        internal ThreadSafeTaskWrapperPromiseNoArgument(Promise<TThisResult> promise, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {
                _Promise.Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}