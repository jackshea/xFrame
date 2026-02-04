using System;
using System.Text;
using NUnit.Framework;
using xFrame.Runtime.Persistence.Security;

namespace xFrame.Tests.PersistenceTests
{
    /// <summary>
    /// 数据安全层单元测试
    /// 测试加密器和校验器的功能
    /// </summary>
    [TestFixture]
    public class SecurityTests
    {
        #region NoEncryptor 测试

        /// <summary>
        /// 测试NoEncryptor名称
        /// </summary>
        [Test]
        public void NoEncryptor_Name_ShouldReturnNone()
        {
            var encryptor = new NoEncryptor();
            Assert.AreEqual("None", encryptor.Name);
        }

        /// <summary>
        /// 测试NoEncryptor加密返回原数据
        /// </summary>
        [Test]
        public void NoEncryptor_Encrypt_ShouldReturnSameData()
        {
            var encryptor = new NoEncryptor();
            var data = Encoding.UTF8.GetBytes("Hello World");

            var encrypted = encryptor.Encrypt(data);

            Assert.AreEqual(data, encrypted);
        }

        /// <summary>
        /// 测试NoEncryptor解密返回原数据
        /// </summary>
        [Test]
        public void NoEncryptor_Decrypt_ShouldReturnSameData()
        {
            var encryptor = new NoEncryptor();
            var data = Encoding.UTF8.GetBytes("Hello World");

            var decrypted = encryptor.Decrypt(data);

            Assert.AreEqual(data, decrypted);
        }

        #endregion

        #region AesEncryptor 测试

        /// <summary>
        /// 测试AesEncryptor名称
        /// </summary>
        [Test]
        public void AesEncryptor_Name_ShouldReturnAES()
        {
            var encryptor = new AesEncryptor("test_password");
            Assert.AreEqual("AES", encryptor.Name);
        }

        /// <summary>
        /// 测试AesEncryptor加密解密往返
        /// </summary>
        [Test]
        public void AesEncryptor_EncryptDecrypt_ShouldPreserveData()
        {
            var encryptor = new AesEncryptor("test_password_123");
            var originalData = Encoding.UTF8.GetBytes("Hello World! 你好世界！");

            var encrypted = encryptor.Encrypt(originalData);
            var decrypted = encryptor.Decrypt(encrypted);

            Assert.AreEqual(originalData, decrypted);
        }

        /// <summary>
        /// 测试AesEncryptor加密后数据不同
        /// </summary>
        [Test]
        public void AesEncryptor_Encrypt_ShouldProduceDifferentData()
        {
            var encryptor = new AesEncryptor("test_password");
            var originalData = Encoding.UTF8.GetBytes("Hello World");

            var encrypted = encryptor.Encrypt(originalData);

            Assert.AreNotEqual(originalData, encrypted);
            Assert.AreNotEqual(originalData.Length, encrypted.Length);
        }

        /// <summary>
        /// 测试不同密钥产生不同加密结果
        /// </summary>
        [Test]
        public void AesEncryptor_DifferentKeys_ShouldProduceDifferentResults()
        {
            var encryptor1 = new AesEncryptor("password1");
            var encryptor2 = new AesEncryptor("password2");
            var data = Encoding.UTF8.GetBytes("Test Data");

            var encrypted1 = encryptor1.Encrypt(data);
            var encrypted2 = encryptor2.Encrypt(data);

            Assert.AreNotEqual(encrypted1, encrypted2);
        }

        /// <summary>
        /// 测试使用盐值的加密器
        /// </summary>
        [Test]
        public void AesEncryptor_WithSalt_ShouldWork()
        {
            var encryptor = new AesEncryptor("password", "custom_salt");
            var data = Encoding.UTF8.GetBytes("Test with salt");

            var encrypted = encryptor.Encrypt(data);
            var decrypted = encryptor.Decrypt(encrypted);

            Assert.AreEqual(data, decrypted);
        }

        /// <summary>
        /// 测试空密钥抛出异常
        /// </summary>
        [Test]
        public void AesEncryptor_NullPassword_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new AesEncryptor(null));
        }

        /// <summary>
        /// 测试空数据加密
        /// </summary>
        [Test]
        public void AesEncryptor_EmptyData_ShouldReturnEmpty()
        {
            var encryptor = new AesEncryptor("password");

            var encrypted = encryptor.Encrypt(Array.Empty<byte>());

            Assert.IsEmpty(encrypted);
        }

        /// <summary>
        /// 测试使用原始密钥和IV
        /// </summary>
        [Test]
        public void AesEncryptor_WithRawKeyAndIv_ShouldWork()
        {
            var key = new byte[32];
            var iv = new byte[16];
            for (int i = 0; i < 32; i++) key[i] = (byte)i;
            for (int i = 0; i < 16; i++) iv[i] = (byte)(i + 100);

            var encryptor = new AesEncryptor(key, iv);
            var data = Encoding.UTF8.GetBytes("Raw key test");

            var encrypted = encryptor.Encrypt(data);
            var decrypted = encryptor.Decrypt(encrypted);

            Assert.AreEqual(data, decrypted);
        }

        /// <summary>
        /// 测试无效密钥长度抛出异常
        /// </summary>
        [Test]
        public void AesEncryptor_InvalidKeyLength_ShouldThrow()
        {
            var invalidKey = new byte[16]; // 应该是32字节
            var iv = new byte[16];

            Assert.Throws<ArgumentException>(() => new AesEncryptor(invalidKey, iv));
        }

        #endregion

        #region Sha256Validator 测试

        /// <summary>
        /// 测试Sha256Validator名称
        /// </summary>
        [Test]
        public void Sha256Validator_Name_ShouldReturnSHA256()
        {
            var validator = new Sha256Validator();
            Assert.AreEqual("SHA256", validator.Name);
        }

        /// <summary>
        /// 测试Sha256Validator计算哈希
        /// </summary>
        [Test]
        public void Sha256Validator_ComputeHash_ShouldReturn32Bytes()
        {
            var validator = new Sha256Validator();
            var data = Encoding.UTF8.GetBytes("Test data");

            var hash = validator.ComputeHash(data);

            Assert.AreEqual(32, hash.Length);
        }

        /// <summary>
        /// 测试Sha256Validator验证哈希
        /// </summary>
        [Test]
        public void Sha256Validator_VerifyHash_ShouldReturnTrue()
        {
            var validator = new Sha256Validator();
            var data = Encoding.UTF8.GetBytes("Test data");

            var hash = validator.ComputeHash(data);
            var result = validator.VerifyHash(data, hash);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// 测试Sha256Validator验证错误哈希
        /// </summary>
        [Test]
        public void Sha256Validator_VerifyHash_WrongHash_ShouldReturnFalse()
        {
            var validator = new Sha256Validator();
            var data = Encoding.UTF8.GetBytes("Test data");
            var wrongHash = new byte[32];

            var result = validator.VerifyHash(data, wrongHash);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// 测试相同数据产生相同哈希
        /// </summary>
        [Test]
        public void Sha256Validator_SameData_ShouldProduceSameHash()
        {
            var validator = new Sha256Validator();
            var data1 = Encoding.UTF8.GetBytes("Same data");
            var data2 = Encoding.UTF8.GetBytes("Same data");

            var hash1 = validator.ComputeHash(data1);
            var hash2 = validator.ComputeHash(data2);

            Assert.AreEqual(hash1, hash2);
        }

        /// <summary>
        /// 测试不同数据产生不同哈希
        /// </summary>
        [Test]
        public void Sha256Validator_DifferentData_ShouldProduceDifferentHash()
        {
            var validator = new Sha256Validator();
            var data1 = Encoding.UTF8.GetBytes("Data 1");
            var data2 = Encoding.UTF8.GetBytes("Data 2");

            var hash1 = validator.ComputeHash(data1);
            var hash2 = validator.ComputeHash(data2);

            Assert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// 测试空数据哈希
        /// </summary>
        [Test]
        public void Sha256Validator_EmptyData_ShouldReturnEmptyHash()
        {
            var validator = new Sha256Validator();

            var hash = validator.ComputeHash(Array.Empty<byte>());

            Assert.IsEmpty(hash);
        }

        #endregion

        #region Crc32Validator 测试

        /// <summary>
        /// 测试Crc32Validator名称
        /// </summary>
        [Test]
        public void Crc32Validator_Name_ShouldReturnCRC32()
        {
            var validator = new Crc32Validator();
            Assert.AreEqual("CRC32", validator.Name);
        }

        /// <summary>
        /// 测试Crc32Validator计算哈希
        /// </summary>
        [Test]
        public void Crc32Validator_ComputeHash_ShouldReturn4Bytes()
        {
            var validator = new Crc32Validator();
            var data = Encoding.UTF8.GetBytes("Test data");

            var hash = validator.ComputeHash(data);

            Assert.AreEqual(4, hash.Length);
        }

        /// <summary>
        /// 测试Crc32Validator验证哈希
        /// </summary>
        [Test]
        public void Crc32Validator_VerifyHash_ShouldReturnTrue()
        {
            var validator = new Crc32Validator();
            var data = Encoding.UTF8.GetBytes("Test data");

            var hash = validator.ComputeHash(data);
            var result = validator.VerifyHash(data, hash);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// 测试Crc32Validator验证错误哈希
        /// </summary>
        [Test]
        public void Crc32Validator_VerifyHash_WrongHash_ShouldReturnFalse()
        {
            var validator = new Crc32Validator();
            var data = Encoding.UTF8.GetBytes("Test data");
            var wrongHash = new byte[] { 0, 0, 0, 0 };

            var result = validator.VerifyHash(data, wrongHash);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// 测试相同数据产生相同CRC32
        /// </summary>
        [Test]
        public void Crc32Validator_SameData_ShouldProduceSameHash()
        {
            var validator = new Crc32Validator();
            var data1 = Encoding.UTF8.GetBytes("Same data");
            var data2 = Encoding.UTF8.GetBytes("Same data");

            var hash1 = validator.ComputeHash(data1);
            var hash2 = validator.ComputeHash(data2);

            Assert.AreEqual(hash1, hash2);
        }

        #endregion

        #region NoValidator 测试

        /// <summary>
        /// 测试NoValidator名称
        /// </summary>
        [Test]
        public void NoValidator_Name_ShouldReturnNone()
        {
            var validator = new NoValidator();
            Assert.AreEqual("None", validator.Name);
        }

        /// <summary>
        /// 测试NoValidator计算哈希返回空
        /// </summary>
        [Test]
        public void NoValidator_ComputeHash_ShouldReturnEmpty()
        {
            var validator = new NoValidator();
            var data = Encoding.UTF8.GetBytes("Test data");

            var hash = validator.ComputeHash(data);

            Assert.IsEmpty(hash);
        }

        /// <summary>
        /// 测试NoValidator验证始终返回true
        /// </summary>
        [Test]
        public void NoValidator_VerifyHash_ShouldAlwaysReturnTrue()
        {
            var validator = new NoValidator();
            var data = Encoding.UTF8.GetBytes("Test data");
            var anyHash = new byte[] { 1, 2, 3, 4 };

            var result = validator.VerifyHash(data, anyHash);

            Assert.IsTrue(result);
        }

        #endregion
    }
}
