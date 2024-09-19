namespace InfernoDispatcher
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
        public PromiseVoid Apply(TParam param) {
            return new PromiseVoid((resolve, reject) => _Func(param, resolve, reject));
        }
    }
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
    public class Promise
    {
        public static PromiseVoid New(Action<Action, Action<Exception>> c)
        {
            return new PromiseVoid(c);
        }
        public static PromiseParametrizedVoid<TArgument> New<TArgument>(Action<TArgument, Action, Action<Exception>> c)
        {
            return new PromiseParametrizedVoid<TArgument>(c);
        }
        public static Promise<TReturn> New<TReturn>(Action<Action<TReturn>, Action<Exception>> c)
        {
            return new Promise<TReturn>(c);
        }
        public static PromiseParametrized<TArgument, TReturn> New<TArgument, TReturn>(Action<TArgument, Action<TReturn>, Action<Exception>> c)
        {
            return new PromiseParametrized<TArgument, TReturn>(c);
        }
    }
}