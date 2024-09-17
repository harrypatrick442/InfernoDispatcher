namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResultArgument<TArgument1, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TThisResult> _Callback;
        internal ThreadSafeTaskWrapperWithResultArgument(Func<TArgument1, TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0]);
                Success(result);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}