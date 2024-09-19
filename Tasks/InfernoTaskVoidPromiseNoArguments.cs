using InfernoDispatcher.Promises;

namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskVoidPromiseNoArguments : InfernoTaskNoResultBase
    {
        private PromiseVoid _Promise;
        internal InfernoTaskVoidPromiseNoArguments(PromiseVoid promise, params InfernoTask[] froms) : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Promise.Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}