/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailStreamReader
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Kingthy.Mail
{
    /// <summary>
    /// 邮件数据流读取器
    /// </summary>
    public class MailStreamReader
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public MailStreamReader(Stream stream)
        {
            this.BaseStream = stream;
            this.buffer = new List<byte>(256);
        }

        /// <summary>
        /// 
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// 数据缓存
        /// </summary>
        private List<byte> buffer;

        /// <summary>
        /// 是否已到流尾部
        /// </summary>
        public bool EOS { get; private set; }

        /// <summary>
        /// 从基础流读取数据并存入缓存
        /// </summary>
        /// <returns></returns>
        private int ReadBuffer()
        {
            byte[] data = new byte[256];
            int size = this.BaseStream.Read(data, 0, data.Length);
            if (size > 0)
            {
                if (size != data.Length)
                {
                    byte[] d = new byte[size];
                    Buffer.BlockCopy(data, 0, d, 0, size);
                    data = d;
                }
                buffer.AddRange(data);
            }
            this.EOS = size == 0;
            return size;
        }

        /// <summary>
        /// 读取一行数据
        /// </summary>
        /// <returns></returns>
        public byte[] ReadLine()
        {
            List<byte> stream = new List<byte>(256);
            this.EOS = false;
            while (buffer.Count != 0 || ReadBuffer() != 0)
            {
                byte b = buffer[0];
                buffer.RemoveAt(0);
                if (b == (byte)'\n') break;          //找到换行符
                if (b != (byte)'\r') stream.Add(b);  //如果非回车符则加入到队列
            }
            return stream.ToArray();
        }

        /// <summary>
        /// 读取多行数据.直到碰到结束符"."为此
        /// </summary>
        /// <returns></returns>
        public byte[] ReadMultiLine()
        {
            List<byte> stream = new List<byte>(256);
            this.EOS = false;
            do
            {
                byte[] buffer = ReadLine();
                if (this.EOS) break;
                if (buffer.Length != 0)
                {
                    //找到结束符"."
                    if (buffer.Length == 1 && buffer[0] == (byte)'.') break;
                    stream.AddRange(buffer);
                }
                //加入回车换行符
                stream.Add((byte)'\r');
                stream.Add((byte)'\n');

            } while (true);
            return stream.ToArray();
        }

        /// <summary>
        /// 读取一行数据.并根据编码将其转换为文本字符串
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ReadLineAsString(Encoding encoding)
        {
            byte[] buffer = ReadLine();
            if (this.EOS) return null;
            if (buffer.Length == 0) return string.Empty;

            return encoding.GetString(buffer);
        }
        /// <summary>
        /// 读取多行数据.直到碰到结束符"."为此.并将数据根据编译将其转换为文本字符串
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ReadMultiLineAsString(Encoding encoding)
        {
            byte[] buffer = ReadMultiLine();
            if (buffer.Length == 0) return string.Empty;

            return encoding.GetString(buffer);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            BaseStream.Close();
        }
    }
}
