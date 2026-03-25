using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace xFrame.Runtime.ResourceManager
{
    /// <summary>
    ///     资源域感知的资源管理器接口。
    ///     为异步加载提供生命周期绑定能力，确保域失效后的晚到结果不会继续分发到业务层。
    /// </summary>
    public interface IResourceDomainAssetManager
    {
        /// <summary>
        ///     创建新的资源域。
        /// </summary>
        /// <param name="name">资源域名称，用于调试和日志定位。</param>
        /// <returns>新创建的资源域实例。</returns>
        ResourceDomain CreateDomain(string name = null);

        /// <summary>
        ///     销毁指定资源域。
        ///     销毁后该域下所有未完成请求都会被标记为失效。
        /// </summary>
        /// <param name="domain">要销毁的资源域。</param>
        void DestroyDomain(ResourceDomain domain);

        /// <summary>
        ///     将资源域推进到下一代生命周期。
        ///     旧代未完成请求仍会保留为无效请求，等待底层加载完成后被统一拦截并回收。
        /// </summary>
        /// <param name="domain">要续代的资源域。</param>
        void RenewDomain(ResourceDomain domain);

        /// <summary>
        ///     在指定资源域内异步加载资源。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="domain">资源域。</param>
        /// <param name="address">资源地址。</param>
        /// <returns>若域在完成时仍有效则返回资源，否则返回 null。</returns>
        Task<T> LoadAssetAsync<T>(ResourceDomain domain, string address) where T : Object;

        /// <summary>
        ///     在指定资源域内异步加载资源。
        /// </summary>
        /// <param name="domain">资源域。</param>
        /// <param name="address">资源地址。</param>
        /// <param name="type">资源类型。</param>
        /// <returns>若域在完成时仍有效则返回资源，否则返回 null。</returns>
        Task<Object> LoadAssetAsync(ResourceDomain domain, string address, Type type);
    }
}
