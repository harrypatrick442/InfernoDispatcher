using InfernoDispatcher.Core;

namespace InfernoDispatcher.Tasks
{
    public abstract class InfernoTaskNoResultBase : InfernoTask
    {
        internal InfernoTaskNoResultBase(params InfernoTask[] froms) : base(froms)
        {

        }
        public void Success()
        {
            Success(null);
        }
        #region Awaitable methods
        public virtual void GetResult()
        {
            lock (_LockObject)
            {
                if (_Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (_Exception != null)
                {
                    ThrowException();
                    return;
                }
            }
        }
        public InfernoTaskNoResultBase GetAwaiter()
        {
            return this;
        }
        #endregion
    }
}