namespace xFrame.MVP
{
    /// <summary>
    /// MVP三元组对象池接口
    /// </summary>
    public interface IMVPTriplePool
    {
        /// <summary>
        /// 从池中获取MVP三元组
        /// </summary>
        TMVPTriple Get<TMVPTriple>() where TMVPTriple : class, IMVPTriple;
        
        /// <summary>
        /// 将MVP三元组返回池中
        /// </summary>
        void Return<TMVPTriple>(TMVPTriple mvpTriple) where TMVPTriple : class, IMVPTriple;
    }
}
