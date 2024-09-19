namespace InfernoDispatcher
{
    public class InfernoTaskVoidPromiseReturnWithArgument<TArgument> : InfernoTaskNoResultBase
    {
        private readonly Func<TArgument, PromiseVoid> _Func;
        internal InfernoTaskVoidPromiseReturnWithArgument(Func<TArgument, PromiseVoid> func, 
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