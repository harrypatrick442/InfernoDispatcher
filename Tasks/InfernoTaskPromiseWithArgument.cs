using InfernoDispatcher.Promises;

namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskPromiseWithArgument<TArgument, TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly PromiseParametrized<TArgument, TThisResult> _Promise;
        internal InfernoTaskPromiseWithArgument(PromiseParametrized<TArgument, TThisResult> promise, params InfernoTask[] froms)
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