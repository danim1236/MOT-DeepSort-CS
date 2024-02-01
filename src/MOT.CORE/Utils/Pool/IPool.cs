namespace MOT.Core.Utils.Pool
{
    public interface IPool<T>
    {
        public abstract PoolObject<T> Get();
        public abstract void Release(PoolObject<T> @object);
    }
}
