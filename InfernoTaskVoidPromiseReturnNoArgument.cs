namespace InfernoDispatcher
{
    public class InfernoTaskVoidPromiseReturnNoArgument : InfernoTaskNoResultBase
    {
        private readonly Func<PromiseVoid> _Func;
        internal InfernoTaskVoidPromiseReturnNoArgument(Func<PromiseVoid> func, 
            params InfernoTask[] froms)
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