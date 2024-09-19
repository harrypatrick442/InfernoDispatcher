namespace InfernoDispatcher.Promises
{
    public class PromiseParametrized<TParam, TResult>
    {
        private Action<TParam, Action<TResult>, Action<Exception>> _Func;
        public PromiseParametrized(Action<TParam, Action<TResult>, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(TParam param, Action<TResult> resolve, Action<Exception> reject)
        {
            _Func(param, resolve, reject);
        }
        public Promise<TResult> Apply(TParam param)
        {
            return new Promise<TResult>((resolve, reject) => _Func(param, resolve, reject));
        }
    }
}