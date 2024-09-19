namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperPromiseReturnWithArgument<TArgument, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TArgument, Promise<TThisResult>> _Func;
        internal ThreadSafeTaskWrapperPromiseReturnWithArgument(Func<TArgument, Promise<TThisResult>> func, 
            params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Func = func;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {
                _Func((TArgument)arguments![0]).Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}