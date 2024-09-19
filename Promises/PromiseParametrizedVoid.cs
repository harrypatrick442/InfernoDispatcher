namespace InfernoDispatcher.Promises
{
    public class PromiseParametrizedVoid<TParam>
    {
        private Action<TParam, Action, Action<Exception>> _Func;
        public PromiseParametrizedVoid(Action<TParam, Action, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(TParam param, Action resolve, Action<Exception> reject)
        {
            _Func(param, resolve, reject);
        }
        public PromiseVoid Apply(TParam param)
        {
            return new PromiseVoid((resolve, reject) => _Func(param, resolve, reject));
        }
    }
}