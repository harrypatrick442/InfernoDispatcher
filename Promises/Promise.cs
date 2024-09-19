namespace InfernoDispatcher.Promises
{
    public class Promise<TResult>
    {
        private Action<Action<TResult>, Action<Exception>> _Func;
        public Promise(Action<Action<TResult>, Action<Exception>> func) : base()
        {
            _Func = func;
        }
        public void Run(Action<TResult> resolve, Action<Exception> reject)
        {
            _Func(resolve, reject);
        }
    }
    public static class Promise
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