namespace InfernoDispatcher
{
    public class InfernoTaskPromiseNoArgument<TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Promise<TThisResult> _Promise;
        internal InfernoTaskPromiseNoArgument(Promise<TThisResult> promise, params InfernoTask[] froms)
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