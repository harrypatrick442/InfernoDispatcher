namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperPromiseReturnNoArgument<TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<Promise<TThisResult>> _Func;
        internal ThreadSafeTaskWrapperPromiseReturnNoArgument(Func<Promise<TThisResult>> func, 
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
                _Func().Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}