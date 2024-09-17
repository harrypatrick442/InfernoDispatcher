namespace InfernoDispatcher
{
    public class Promise<TParam, TResult>
    {
        private Action<TParam, Action<TResult>, Action<Exception>> _Func;
        public Promise(Action<TParam, Action<TResult>, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(TParam param, Action<TResult> resolve, Action<Exception> reject)
        {
            _Func(param, resolve, reject);
        }
        public Promise<TResult> Apply(TParam param) {
            return new Promise<TResult>((resolve, reject) => _Func(param, resolve, reject));
        }
    }
    public class Promise<TResult>
    {
        private Action<Action<TResult>, Action<Exception>> _Func;
        public Promise(Action<Action<TResult>, Action<Exception>> func):base()
        {
            _Func = func;
        }
        public void Run(Action<TResult> resolve, Action<Exception> reject)
        {
            _Func(resolve, reject);
        }
    }
    public class VoidPromise<TParam>
    {
        private Action<TParam, Action, Action<Exception>> _Func;
        public VoidPromise(Action<TParam, Action, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(TParam param, Action resolve, Action<Exception> reject)
        {
            _Func(param, resolve, reject);
        }
        public VoidPromise Apply(TParam param) {
            return new VoidPromise((resolve, reject) => _Func(param, resolve, reject));
        }
    }
    public class VoidPromise
    {
        private Action<Action, Action<Exception>> _Func;
        public VoidPromise(Action<Action, Action<Exception>> func)
        {
            _Func = func;
        }
        public void Run(Action resolve, Action<Exception> reject)
        {
            _Func(resolve, reject);
        }
    }
}