using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiiAsset.Runtime
{
    public static class AESEncrypter
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("1234567890123456"); // 16字节密钥
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("6543210987654323"); // 16字节IV

        private static readonly string OKey = "f:~fDFceUQYEP&";

        private static Dictionary<string, string> _recoverRecord = new();

        /// <summary>
        /// 加密 JSON 并写入文件
        /// </summary>
        public static void EncryptAndSave(string inputFile, string outputFile)
        {
            var ext = Path.GetExtension(inputFile);
            var backUpFilePath = "";
            switch (ext)
            {
                case ".json":
                    var jsonContent = File.ReadAllText(inputFile);
                    var encryptedJson = Encrypt(jsonContent, inputFile);

                    backUpFilePath = inputFile.Replace(".json", "-backup.json");
                    backUpFilePath = backUpFilePath.Replace("Bundles", "BackUp");
                    Directory.CreateDirectory(Path.GetDirectoryName(backUpFilePath));
                    Console.WriteLine($"[AESEncrypter]写入备份：{backUpFilePath}");

                    File.WriteAllText(backUpFilePath, jsonContent);
                    _recoverRecord[backUpFilePath] = inputFile;

                    File.WriteAllText(outputFile, encryptedJson);
                    break;

                case ".txt":
                    var txtContent = File.ReadAllText(inputFile);
                    var encryptedTxt = Encrypt(txtContent, inputFile);

                    backUpFilePath = inputFile.Replace(".txt", "-backup.txt");
                    backUpFilePath = backUpFilePath.Replace("Bundles", "BackUp");
                    Directory.CreateDirectory(Path.GetDirectoryName(backUpFilePath));
                    Console.WriteLine($"[AESEncrypter]写入备份：{backUpFilePath}");

                    File.WriteAllText(backUpFilePath, txtContent);
                    _recoverRecord[backUpFilePath] = inputFile;

                    File.WriteAllText(outputFile, encryptedTxt);
                    break;

                default:
                    // Debug.Log($"[AESEncrypter]未处理的文件类型{ext}");
                    break;
            }

            Console.WriteLine("加密后的 JSON 已保存到: " + outputFile);
        }

        /// <summary>
        /// 加密时会修改本地的文件，打完资源包后需要恢复回去
        /// </summary>
        public static void RecoverFile()
        {
            foreach (var kvPair in _recoverRecord)
            {
                var backup = kvPair.Key;
                var origin = kvPair.Value;
                var oldContent = File.ReadAllText(backup);
                File.WriteAllText(origin, oldContent);
                File.Delete(backup);
            }

            _recoverRecord.Clear();

#if UNITY_EDITOR
            // Directory.Delete("Assets/BackUp/", true);
            AssetDatabase.DeleteAsset("Assets/BackUp/");
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// 读取加密的 JSON 文件并解密
        /// </summary>
        private static void DecryptFile(string encryptedFile)
        {
            string fileContent = File.ReadAllText(encryptedFile);
            JObject jsonObject = JObject.Parse(fileContent);

            if (jsonObject.TryGetValue("d", out JToken encryptedJson))
            {
                string decryptedJson = Decrypt(encryptedJson.ToString(), encryptedFile);
                Console.WriteLine("解密后的 JSON 内容: " + decryptedJson);
            }
            else
            {
                Console.WriteLine("错误: JSON 文件中未找到加密数据！");
            }
        }

        /// <summary>
        /// AES 加密
        /// </summary>
        private static string Encrypt(string plainText, string pkey)
        {
            var result = XOREncryptDecrypt(plainText, OKey);
            var p = XOREncryptDecrypt(pkey, OKey);
            result = p + result;
            return result;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // aesAlg.Mode = EncryptionMode;
                // aesAlg.Padding = PaddingType;

                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        public static string Decrypt(string cipherText, string pkey)
        {
            var p = XOREncryptDecrypt(pkey, OKey);
            if (!cipherText.StartsWith(p))
            {
                return cipherText;
            }

            cipherText = cipherText.Remove(0, p.Length);
            return XOREncryptDecrypt(cipherText, OKey);

            // 创建一个足够大的字节数组来存储解码结果
            Span<byte> buffer = new byte[cipherText.Length * 3 / 4]; // Base64 解码后的最大长度

            // 尝试将 Base64 字符串解码为字节数组
            if (!Convert.TryFromBase64String(cipherText, buffer, out int bytesWritten))
            {
                // 解码失败
                return cipherText;
            }

            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    // aesAlg.Mode = EncryptionMode;
                    // aesAlg.Padding = PaddingType;

                    using (MemoryStream msDecrypt = new MemoryStream(buffer.Slice(0, bytesWritten).ToArray()))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException e)
            {
                // 如果解密失败（例如密钥或 IV 不匹配）
                throw;
            }
            catch (Exception e)
            {
                // 其他异常情况
                throw;
            }
        }

        // XOR 加密/解密方法
        private static string XOREncryptDecrypt(string input, string key)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                // 对每个字符与密钥的对应字符进行 XOR 运算
                char encryptedChar = (char)(input[i] ^ key[i % key.Length]);
                output.Append(encryptedChar);
            }

            return output.ToString();
        }

        /// <summary>
        /// 递归删除空文件夹
        /// </summary>
        /// <param name="directoryPath">要删除的文件夹路径</param>
        public static void DeleteEmptyDirectories(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory does not exist: {directoryPath}");
                return;
            }

            // 递归删除子文件夹
            foreach (var subDirectory in Directory.GetDirectories(directoryPath))
            {
                DeleteEmptyDirectories(subDirectory);
            }

            // 检查当前文件夹是否为空
            if (IsDirectoryEmpty(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath);
                    Console.WriteLine($"Deleted empty directory: {directoryPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete directory {directoryPath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查文件夹是否为空
        /// </summary>
        /// <param name="directoryPath">文件夹路径</param>
        /// <returns>如果文件夹为空，返回 true；否则返回 false</returns>
        private static bool IsDirectoryEmpty(string directoryPath)
        {
            // 检查是否有文件
            if (Directory.GetFiles(directoryPath).Length > 0)
            {
                return false;
            }

            // 检查是否有子文件夹
            if (Directory.GetDirectories(directoryPath).Length > 0)
            {
                return false;
            }

            return true;
        }
    }
}