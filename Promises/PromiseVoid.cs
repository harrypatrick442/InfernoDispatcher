namespace InfernoDispatcher.Promises
{
    public class PromiseVoid
    {
        private Action<Action, Action<Exception>> _Func;
        public PromiseVoid(Action<Action, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(Action resolve, Action<Exception> reject)
        {
            _Func(resolve, reject);
        }
    }
}