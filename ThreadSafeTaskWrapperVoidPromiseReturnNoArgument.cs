namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperVoidPromiseReturnNoArgument : ThreadSafeTaskWrapperNoResultBase
    {
        private readonly Func<PromiseVoid> _Func;
        internal ThreadSafeTaskWrapperVoidPromiseReturnNoArgument(Func<PromiseVoid> func, 
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