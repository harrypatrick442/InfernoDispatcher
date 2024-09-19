using InfernoDispatcher.Promises;

namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskVoidPromiseWithArgument<TArgument> : InfernoTaskNoResultBase
    {
        private PromiseParametrizedVoid<TArgument> _Promise;
        internal InfernoTaskVoidPromiseWithArgument(PromiseParametrizedVoid<TArgument> promise, params InfernoTask[] froms) : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Promise.Run((TArgument)arguments![0], Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}