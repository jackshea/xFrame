using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using xFrame.Runtime.Persistence.Security;
using xFrame.Runtime.Persistence.Storage;
using xFrame.Runtime.Serialization;

namespace xFrame.Tests.PersistenceTests
{
    /// <summary>
    /// 存储介质层单元测试
    /// 测试各种持久化提供者的功能
    /// </summary>
    [TestFixture]
    public class StorageTests
    {
        /// <summary>
        /// 测试用的简单数据类
        /// </summary>
        [Serializable]
        private class TestData
        {
            public string Name;
            public int Value;
        }

        private ISerializer _serializer;
        private string _testBasePath;

        [SetUp]
        public void SetUp()
        {
            _serializer = new JsonSerializer();
            _testBasePath = Path.Combine(Path.GetTempPath(), "xFrame_PersistenceTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试目录
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }

        #region MemoryPersistenceProvider 测试

        /// <summary>
        /// 测试MemoryPersistenceProvider名称
        /// </summary>
        [Test]
        public void MemoryProvider_Name_ShouldReturnMemory()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            Assert.AreEqual("Memory", provider.Name);
        }

        /// <summary>
        /// 测试保存和加载数据
        /// </summary>
        [Test]
        public void MemoryProvider_SaveAndLoad_ShouldWork()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            var data = new TestData { Name = "Test", Value = 42 };

            provider.Save("test_key", data);
            var loaded = provider.Load<TestData>("test_key");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Test", loaded.Name);
            Assert.AreEqual(42, loaded.Value);
        }

        /// <summary>
        /// 测试检查数据存在
        /// </summary>
        [Test]
        public void MemoryProvider_Exists_ShouldReturnCorrectly()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            var data = new TestData { Name = "Test", Value = 1 };

            Assert.IsFalse(provider.Exists("test_key"));

            provider.Save("test_key", data);

            Assert.IsTrue(provider.Exists("test_key"));
        }

        /// <summary>
        /// 测试删除数据
        /// </summary>
        [Test]
        public void MemoryProvider_Delete_ShouldWork()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            var data = new TestData { Name = "Test", Value = 1 };

            provider.Save("test_key", data);
            Assert.IsTrue(provider.Exists("test_key"));

            var result = provider.Delete("test_key");

            Assert.IsTrue(result);
            Assert.IsFalse(provider.Exists("test_key"));
        }

        /// <summary>
        /// 测试删除不存在的数据
        /// </summary>
        [Test]
        public void MemoryProvider_Delete_NonExistent_ShouldReturnFalse()
        {
            var provider = new MemoryPersistenceProvider(_serializer);

            var result = provider.Delete("non_existent_key");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// 测试加载不存在的数据
        /// </summary>
        [Test]
        public void MemoryProvider_Load_NonExistent_ShouldReturnDefault()
        {
            var provider = new MemoryPersistenceProvider(_serializer);

            var loaded = provider.Load<TestData>("non_existent_key");

            Assert.IsNull(loaded);
        }

        /// <summary>
        /// 测试保存原始字节数据
        /// </summary>
        [Test]
        public void MemoryProvider_SaveRawAndLoadRaw_ShouldWork()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            var data = Encoding.UTF8.GetBytes("Raw data test");

            provider.SaveRaw("raw_key", data);
            var loaded = provider.LoadRaw("raw_key");

            Assert.AreEqual(data, loaded);
        }

        /// <summary>
        /// 测试清除所有数据
        /// </summary>
        [Test]
        public void MemoryProvider_Clear_ShouldRemoveAllData()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            provider.Save("key1", new TestData { Name = "1" });
            provider.Save("key2", new TestData { Name = "2" });

            Assert.AreEqual(2, provider.Count);

            provider.Clear();

            Assert.AreEqual(0, provider.Count);
            Assert.IsFalse(provider.Exists("key1"));
            Assert.IsFalse(provider.Exists("key2"));
        }

        /// <summary>
        /// 测试获取所有键
        /// </summary>
        [Test]
        public void MemoryProvider_GetAllKeys_ShouldReturnAllKeys()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            provider.Save("key1", new TestData { Name = "1" });
            provider.Save("key2", new TestData { Name = "2" });

            var keys = provider.GetAllKeys();

            Assert.AreEqual(2, keys.Count);
            Assert.IsTrue(keys.Contains("key1"));
            Assert.IsTrue(keys.Contains("key2"));
        }

        /// <summary>
        /// 测试数据隔离（修改返回的数据不影响存储）
        /// </summary>
        [Test]
        public void MemoryProvider_DataIsolation_ShouldWork()
        {
            var provider = new MemoryPersistenceProvider(_serializer);
            var originalData = Encoding.UTF8.GetBytes("Original");

            provider.SaveRaw("key", originalData);
            var loaded = provider.LoadRaw("key");

            // 修改加载的数据
            loaded[0] = 0xFF;

            // 重新加载应该是原始数据
            var reloaded = provider.LoadRaw("key");
            Assert.AreEqual((byte)'O', reloaded[0]);
        }

        #endregion

        #region FilePersistenceProvider 测试

        /// <summary>
        /// 测试FilePersistenceProvider名称
        /// </summary>
        [Test]
        public void FileProvider_Name_ShouldReturnFile()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            Assert.AreEqual("File", provider.Name);
        }

        /// <summary>
        /// 测试保存和加载数据
        /// </summary>
        [Test]
        public void FileProvider_SaveAndLoad_ShouldWork()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            var data = new TestData { Name = "FileTest", Value = 100 };

            provider.Save("test_key", data);
            var loaded = provider.Load<TestData>("test_key");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("FileTest", loaded.Name);
            Assert.AreEqual(100, loaded.Value);
        }

        /// <summary>
        /// 测试文件是否被创建
        /// </summary>
        [Test]
        public void FileProvider_Save_ShouldCreateFile()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath, ".json");
            var data = new TestData { Name = "Test" };

            provider.Save("test_file", data);

            var filePath = Path.Combine(_testBasePath, "test_file.json");
            Assert.IsTrue(File.Exists(filePath));
        }

        /// <summary>
        /// 测试检查数据存在
        /// </summary>
        [Test]
        public void FileProvider_Exists_ShouldReturnCorrectly()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);

            Assert.IsFalse(provider.Exists("test_key"));

            provider.Save("test_key", new TestData { Name = "Test" });

            Assert.IsTrue(provider.Exists("test_key"));
        }

        /// <summary>
        /// 测试删除数据
        /// </summary>
        [Test]
        public void FileProvider_Delete_ShouldWork()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            provider.Save("test_key", new TestData { Name = "Test" });

            var result = provider.Delete("test_key");

            Assert.IsTrue(result);
            Assert.IsFalse(provider.Exists("test_key"));
        }

        /// <summary>
        /// 测试自定义文件扩展名
        /// </summary>
        [Test]
        public void FileProvider_CustomExtension_ShouldWork()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath, ".save");
            provider.Save("game_data", new TestData { Name = "Game" });

            var filePath = Path.Combine(_testBasePath, "game_data.save");
            Assert.IsTrue(File.Exists(filePath));
        }

        /// <summary>
        /// 测试获取所有键
        /// </summary>
        [Test]
        public void FileProvider_GetAllKeys_ShouldReturnAllKeys()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            provider.Save("key1", new TestData { Name = "1" });
            provider.Save("key2", new TestData { Name = "2" });

            var keys = provider.GetAllKeys();

            Assert.AreEqual(2, keys.Length);
        }

        /// <summary>
        /// 测试清除所有数据
        /// </summary>
        [Test]
        public void FileProvider_Clear_ShouldRemoveAllFiles()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            provider.Save("key1", new TestData { Name = "1" });
            provider.Save("key2", new TestData { Name = "2" });

            provider.Clear();

            Assert.IsFalse(provider.Exists("key1"));
            Assert.IsFalse(provider.Exists("key2"));
        }

        /// <summary>
        /// 测试特殊字符键名处理
        /// </summary>
        [Test]
        public void FileProvider_SpecialCharacterKey_ShouldBeSanitized()
        {
            var provider = new FilePersistenceProvider(_serializer, _testBasePath);
            var data = new TestData { Name = "Special" };

            // 包含非法文件名字符的键
            provider.Save("test/key:with*special?chars", data);

            Assert.IsTrue(provider.Exists("test/key:with*special?chars"));
            var loaded = provider.Load<TestData>("test/key:with*special?chars");
            Assert.AreEqual("Special", loaded.Name);
        }

        #endregion

        #region EncryptedFilePersistenceProvider 测试

        /// <summary>
        /// 测试EncryptedFilePersistenceProvider名称
        /// </summary>
        [Test]
        public void EncryptedFileProvider_Name_ShouldReturnEncryptedFile()
        {
            var encryptor = new AesEncryptor("test_password");
            var provider = new EncryptedFilePersistenceProvider(_serializer, _testBasePath, encryptor);
            Assert.AreEqual("EncryptedFile", provider.Name);
        }

        /// <summary>
        /// 测试加密保存和加载数据
        /// </summary>
        [Test]
        public void EncryptedFileProvider_SaveAndLoad_ShouldWork()
        {
            var encryptor = new AesEncryptor("test_password");
            var provider = new EncryptedFilePersistenceProvider(_serializer, _testBasePath, encryptor);
            var data = new TestData { Name = "Encrypted", Value = 999 };

            provider.Save("encrypted_key", data);
            var loaded = provider.Load<TestData>("encrypted_key");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Encrypted", loaded.Name);
            Assert.AreEqual(999, loaded.Value);
        }

        /// <summary>
        /// 测试文件内容是加密的
        /// </summary>
        [Test]
        public void EncryptedFileProvider_FileContent_ShouldBeEncrypted()
        {
            var encryptor = new AesEncryptor("test_password");
            var provider = new EncryptedFilePersistenceProvider(_serializer, _testBasePath, encryptor, ".enc");
            var data = new TestData { Name = "SecretData", Value = 123 };

            provider.Save("secret", data);

            var filePath = Path.Combine(_testBasePath, "secret.enc");
            var fileContent = File.ReadAllText(filePath);

            // 文件内容不应该包含明文
            Assert.IsFalse(fileContent.Contains("SecretData"));
            Assert.IsFalse(fileContent.Contains("123"));
        }

        /// <summary>
        /// 测试使用错误密钥解密失败
        /// </summary>
        [Test]
        public void EncryptedFileProvider_WrongKey_ShouldFail()
        {
            var encryptor1 = new AesEncryptor("correct_password");
            var encryptor2 = new AesEncryptor("wrong_password");

            var provider1 = new EncryptedFilePersistenceProvider(_serializer, _testBasePath, encryptor1);
            var provider2 = new EncryptedFilePersistenceProvider(_serializer, _testBasePath, encryptor2);

            var data = new TestData { Name = "Test", Value = 1 };
            provider1.Save("key", data);

            // 使用错误密钥解密应该抛出异常
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            {
                provider2.Load<TestData>("key");
            });
        }

        #endregion
    }
}
