using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using XLua;

[LuaCallCSharp]
public class SystemUtils
{
    public static Encoding fileEncoding = new UTF8Encoding(false);
    static MD5 md5 = MD5.Create();

    public static string ReadFileText(string path)
    {
        return File.ReadAllText(path, fileEncoding);
    }
    public static byte[] ReadFileBytes(string path)
    {
        return File.ReadAllBytes(path);
    }
    
    public static void WriteFileByString(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using( StreamWriter sw = File.CreateText(path))
        {
            sw.Write(content);
        }
    }
    public static void WriteFileByBytes(string path, byte[] content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using (FileStream fs = File.Create(path))
        {
            fs.Write(content, 0, content.Length);
        }
    }
    public static string ComputeFileMD5(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return ComputeBytesMD5(bytes);
    }
    public static string ComputeBytesMD5(byte[] bytes)
    {
        byte[] hashcode = md5.ComputeHash(bytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashcode)
        {
            // 将字节转换成16进制表示的字符串，
            sb.Append(b.ToString("x2"));
        }
        
        return sb.ToString();
    }
}