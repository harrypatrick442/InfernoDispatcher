using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResult<TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TThisResult> _Callback;
        internal ThreadSafeTaskWrapperWithResult(Func<TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback();
                Success(result);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}