using System;
using UnityEngine;
using VContainer;
using xFrame.Runtime.Persistence.Migration;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Persistence.Storage;
using xFrame.Runtime.Serialization;

namespace xFrame.Runtime.Persistence
{
    /// <summary>
    /// 持久化模块的VContainer注册扩展方法
    /// </summary>
    public static class PersistenceServiceExtensions
    {
        /// <summary>
        /// 注册持久化模块到VContainer容器（使用默认配置）
        /// </summary>
        /// <param name="builder">容器构建器</param>
        /// <param name="basePath">基础存储路径（默认使用Application.persistentDataPath）</param>
        public static void RegisterPersistenceModule(this IContainerBuilder builder, string basePath = null)
        {
            var path = basePath ?? Application.persistentDataPath;
            var config = PersistenceConfig.CreateDefault(path);
            RegisterPersistenceModule(builder, config);
        }

        /// <summary>
        /// 注册持久化模块到VContainer容器（使用自定义配置）
        /// </summary>
        /// <param name="builder">容器构建器</param>
        /// <param name="config">持久化配置</param>
        public static void RegisterPersistenceModule(this IContainerBuilder builder, PersistenceConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // 注册配置
            builder.RegisterInstance(config);

            // 注册迁移管理器
            builder.Register<MigrationManager>(Lifetime.Singleton);

            // 注册校验器
            RegisterValidator(builder, config.ValidatorType);

            // 注册加密器
            if (config.EnableEncryption && !string.IsNullOrEmpty(config.EncryptionKey))
            {
                builder.Register<IEncryptor>(container =>
                {
                    return new AesEncryptor(config.EncryptionKey, config.EncryptionSalt);
                }, Lifetime.Singleton);
            }
            else
            {
                builder.Register<IEncryptor, NoEncryptor>(Lifetime.Singleton);
            }

            // 注册持久化提供者
            RegisterProvider(builder, config);

            // 注册持久化管理器
            builder.Register<IPersistenceManager>(container =>
            {
                var provider = container.Resolve<IPersistenceProvider>();
                var serializer = container.Resolve<ISerializerManager>().DefaultSerializer;
                var encryptor = container.Resolve<IEncryptor>();
                var validator = container.Resolve<IValidator>();

                return new PersistenceManager(provider, serializer, config, encryptor, validator);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// 注册安全持久化模块（启用加密和校验）
        /// </summary>
        /// <param name="builder">容器构建器</param>
        /// <param name="encryptionKey">加密密钥</param>
        /// <param name="basePath">基础存储路径（默认使用Application.persistentDataPath）</param>
        public static void RegisterSecurePersistenceModule(
            this IContainerBuilder builder,
            string encryptionKey,
            string basePath = null)
        {
            var path = basePath ?? Application.persistentDataPath;
            var config = PersistenceConfig.CreateSecure(path, encryptionKey);
            RegisterPersistenceModule(builder, config);
        }

        /// <summary>
        /// 注册校验器
        /// </summary>
        private static void RegisterValidator(IContainerBuilder builder, ValidatorType type)
        {
            switch (type)
            {
                case ValidatorType.None:
                    builder.Register<IValidator, NoValidator>(Lifetime.Singleton);
                    break;
                case ValidatorType.Crc32:
                    builder.Register<IValidator, Crc32Validator>(Lifetime.Singleton);
                    break;
                case ValidatorType.Sha256:
                default:
                    builder.Register<IValidator, Sha256Validator>(Lifetime.Singleton);
                    break;
            }
        }

        /// <summary>
        /// 注册持久化提供者
        /// </summary>
        private static void RegisterProvider(IContainerBuilder builder, PersistenceConfig config)
        {
            if (config.EnableEncryption)
            {
                builder.Register<IPersistenceProvider>(container =>
                {
                    var serializer = container.Resolve<ISerializerManager>().DefaultSerializer;
                    var encryptor = container.Resolve<IEncryptor>();
                    return new EncryptedFilePersistenceProvider(
                        serializer,
                        config.BasePath,
                        encryptor,
                        config.FileExtension);
                }, Lifetime.Singleton);
            }
            else
            {
                builder.Register<IPersistenceProvider>(container =>
                {
                    var serializer = container.Resolve<ISerializerManager>().DefaultSerializer;
                    return new FilePersistenceProvider(
                        serializer,
                        config.BasePath,
                        config.FileExtension);
                }, Lifetime.Singleton);
            }
        }

        /// <summary>
        /// 注册内存持久化提供者（用于测试）
        /// </summary>
        /// <param name="builder">容器构建器</param>
        public static void RegisterMemoryPersistenceProvider(this IContainerBuilder builder)
        {
            builder.Register<IPersistenceProvider>(container =>
            {
                var serializer = container.Resolve<ISerializerManager>().DefaultSerializer;
                return new MemoryPersistenceProvider(serializer);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// 注册自定义持久化提供者
        /// </summary>
        /// <typeparam name="TProvider">提供者类型</typeparam>
        /// <param name="builder">容器构建器</param>
        public static void RegisterPersistenceProvider<TProvider>(this IContainerBuilder builder)
            where TProvider : class, IPersistenceProvider
        {
            builder.Register<IPersistenceProvider, TProvider>(Lifetime.Singleton);
        }

        /// <summary>
        /// 注册自定义加密器
        /// </summary>
        /// <typeparam name="TEncryptor">加密器类型</typeparam>
        /// <param name="builder">容器构建器</param>
        public static void RegisterEncryptor<TEncryptor>(this IContainerBuilder builder)
            where TEncryptor : class, IEncryptor
        {
            builder.Register<IEncryptor, TEncryptor>(Lifetime.Singleton);
        }

        /// <summary>
        /// 注册自定义校验器
        /// </summary>
        /// <typeparam name="TValidator">校验器类型</typeparam>
        /// <param name="builder">容器构建器</param>
        public static void RegisterValidator<TValidator>(this IContainerBuilder builder)
            where TValidator : class, IValidator
        {
            builder.Register<IValidator, TValidator>(Lifetime.Singleton);
        }
    }
}
