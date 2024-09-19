namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperPromiseWithArgument<TArgument, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly PromiseParametrized<TArgument, TThisResult> _Promise;
        internal ThreadSafeTaskWrapperPromiseWithArgument(PromiseParametrized<TArgument, TThisResult> promise, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {
                _Promise.Run((TArgument)arguments![0], Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}