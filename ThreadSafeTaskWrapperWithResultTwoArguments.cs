namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResultTwoArguments<TArgument1, TArgument2, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TArgument2, TThisResult> _Callback;
        internal ThreadSafeTaskWrapperWithResultTwoArguments(Func<TArgument1, TArgument2, TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0], (TArgument2)arguments![1]);
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