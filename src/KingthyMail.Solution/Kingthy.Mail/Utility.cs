/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  Utility
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.IO;
using System.Globalization;
using System.Net.Mime;
using Kingthy.Mail.Mime;

namespace Kingthy.Mail
{
    /// <summary>
    /// 实用类
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// 将字符串转换为Int32
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int ToInt32(string value)
        {
            int v;
            if (int.TryParse(value, out v)) return v;
            return 0;
        }

        /// <summary>
        /// 将字符串转换为MailAddress
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static MailAddress ToMailAddress(string value)
        {
            value = value.Trim();
            try
            {
                return new MailAddress(value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将字符串转换为日期时间
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static DateTime ToDateTime(string value)
        {
            //替换掉日期后面的时间格式字符.如:"(PDT)","(PST)","(CST)"
            value = Regex.Replace(value, @"\s*\([^\)]+\)\s*$", "");
            DateTime time;
            if (!DateTime.TryParse(value, out time)) time = DateTime.MinValue;
            return time;
        }

        /// <summary>
        /// 根据字符集获取编码
        /// </summary>
        /// <param name="charset"></param>
        /// <returns></returns>
        internal static Encoding GetEncoding(string charset)
        {
            Encoding e = Encoding.ASCII;
            if (!string.IsNullOrEmpty(charset))
            {
                try
                {
                    if ("GBK".Equals(charset, StringComparison.InvariantCultureIgnoreCase)) charset = "GB18030";
                    e = Encoding.GetEncoding(charset);
                }
                catch
                {
                    e = Encoding.UTF8;
                }
            }
            return e;
        }

        /// <summary>
        /// 添加邮件列表
        /// </summary>
        /// <param name="container"></param>
        /// <param name="addresses"></param>
        internal static void AddMailAddresses(MailAddressCollection container, string addresses)
        {
            MailAddress adr;
            try
            {
                string[] items = addresses.Split(',');
                foreach (string item in items)
                {
                    adr = ToMailAddress(item);
                    if (adr != null)
                    {
                        container.Add(adr);
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// 解码Mime头的值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string DecodeHeaderValue(string value)
        {
            Encoding e;
            return DecodeHeaderValue(value, out e);
        }
        /// <summary>
        /// 解码Mime头的值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        internal static string DecodeHeaderValue(string value, out Encoding en)
        {
            Encoding e = Encoding.ASCII;
            value = Regex.Replace(value, @"=\?(?<charset>[^\?]+)\?(?<encoding>[^\?]+)\?(?<text>[^\?]+)\?=", m =>
            {
                string charset = m.Groups["charset"].Value;
                string encoding = m.Groups["encoding"].Value;
                string text = m.Groups["text"].Value;

                e = GetEncoding(charset);
                TransferEncoding te = TransferEncoding.SevenBit;
                if (encoding.Equals("B", StringComparison.InvariantCultureIgnoreCase))
                {
                    te = TransferEncoding.Base64;
                }
                else if (encoding.Equals("Q", StringComparison.InvariantCultureIgnoreCase))
                {
                    te = TransferEncoding.QuotedPrintable;
                }
                return DecodeMimeText(text, te, e);
            });
            en = e;

            return value;
        }

        /// <summary>
        /// 对内容解码
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        internal static string DecodeMimeText(string text, TransferEncoding encoding, Encoding charset)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            switch (encoding)
            {
                case TransferEncoding.Base64:
                    return charset.GetString(Convert.FromBase64String(text));
                case TransferEncoding.QuotedPrintable:
                    return DecodeQPString(text, charset);
                default:
                    return text;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        internal static string EncodeMimeText(Stream stream, TransferEncoding encoding, Encoding charset)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            string text = string.Empty;
            switch (encoding)
            {
                case TransferEncoding.Base64:
                    text = Convert.ToBase64String(buffer, Base64FormattingOptions.InsertLineBreaks);
                    break;
                default:
                    text = charset.GetString(buffer);
                    break;
            }
            return text;
        }

        /// <summary>
        /// 对QuotedPrintable编码字符进行解码
        /// </summary>
        /// <param name="value"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string DecodeQPString(string value, Encoding charset)
        {
            StringBuilder buffer = new StringBuilder();
            int offset = 0;
            bool error = false;
            List<byte> bits = new List<byte>();
            while (offset < value.Length)
            {
                char c = value[offset ++];
                if (c == '=')
                {
                    //获取到编码字符
                    char c1 = offset < value.Length ? value[offset ++] : '\0';
                    char c2 = offset < value.Length ? value[offset ++] : '\0';
                    if (IsHexNumber(c1) && IsHexNumber(c2))
                    {
                        bits.Add(byte.Parse(string.Concat(c1, c2), NumberStyles.HexNumber));
                    }
                    else if ((c1 == '\r' && c2 == '\n') || (c1 == '\n' && c2 == '='))
                    {
                        if (c2 == '=') offset--;
                    }
                    else
                    {
                        //"="后跟其它字符则有错误
                        if(c1 != '\0') error = true;
                        break;
                    }
                }
                else
                {
                    if (bits.Count > 0)
                    {
                        buffer.Append(charset.GetString(bits.ToArray()));
                        bits.Clear();
                    }
                    buffer.Append(c);
                }
            }

            if (error)
            {
                //有错误.则返回原字符串
                buffer.Length = 0;
                buffer.Append(value);
            }
            else if (bits.Count > 0)
            {
                buffer.Append(charset.GetString(bits.ToArray()));
                bits.Clear();

            }
            return buffer.ToString();
        }
        /// <summary>
        /// 是否是十六进制字符
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsHexNumber(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        #region IMAP4 相关的函数
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <param name="options"></param>
        /// <param name="find"></param>
        /// <returns></returns>
        internal static bool FindMatchItem(string text, string pattern, RegexOptions options, out string find)
        {
            find = null;
            Match m = Regex.Match(text, pattern, options);
            if (m.Success)
            {
                find = m.Groups["item"].Success ? m.Groups["item"].Value : m.Groups[1].Value;
            }
            return m.Success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="data"></param>
        internal static void ParseIMAP4MailStatus(IMAP4.IMAP4MailboxStatus status, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                using (StringReader reader = new StringReader(data))
                {
                    while (reader.Peek() != -1)
                    {
                        string line = reader.ReadLine().Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] items = line.Split(new char[] { ' ' }, 3);
                            if (items.Length > 2)
                            {
                                if ("EXISTS".Equals(items[2], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    status.TotalMessages = Utility.ToInt32(items[1]);
                                }
                                else if ("RECENT".Equals(items[2], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    status.TotalRecentMessages = Utility.ToInt32(items[1]);
                                }
                                else if ("FLAGS".Equals(items[1], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    status.Flags = items[2].Trim("()".ToCharArray());
                                }
                                else
                                {
                                    string item;
                                    if (FindMatchItem(items[2], @"^\[UNSEEN\s+(\d+)\s*\]", RegexOptions.IgnoreCase, out item))
                                    {
                                        status.TotalUnseenMessages = Utility.ToInt32(item);
                                    }
                                    else if (FindMatchItem(items[2], @"^\[UIDVALIDITY\s+([^\]]+)\s*\]", RegexOptions.IgnoreCase, out item))
                                    {
                                        status.ValidityUID = item;
                                    }
                                    else if (FindMatchItem(items[2], @"^\[UIDNEXT\s+([^\]]+)\s*\]", RegexOptions.IgnoreCase, out item))
                                    {
                                        status.NextUID = item;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解析邮件消息
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="data"></param>
        internal static void ParseIMAP4MailMessages(List<MailMessage> messages, string data)
        {
            List<string> contents = ParseIMAP4MailContents(data);
            foreach (string content in contents)
            {
                messages.Add(MimeParser.ParseFromString(content));
            }
        }
        /// <summary>
        /// 解析邮件内容
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static List<string> ParseIMAP4MailContents(string data)
        {
            List<string> contents = new List<string>();
            using (StringReader reader = new StringReader(data))
            {
                Regex regex = new Regex(@"^[*=]\s+[^\s]+\s+FETCH\s+\(BODY\[[^\]]*\]\s+{(\d+)}$", RegexOptions.IgnoreCase);
                while (reader.Peek() != -1)
                {
                    string line = reader.ReadLine();
                    //只解析"*  序号 FETCH (BODY[HEADER] {长度}" 或 "* 序号 FETCH (BODY[] {长度}"的格式
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        int length = Utility.ToInt32(m.Groups[1].Value);
                        char[] buffer = new char[length];
                        length = reader.Read(buffer, 0, length);
                        if (length > 0)
                        {
                            contents.Add(new string(buffer, 0 , length));
                        }
                    }
                }
            }
            return contents;
        }

        /// <summary>
        /// 解析邮件列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mailboxes"></param>
        /// <param name="data"></param>
        internal static void ParseIMAP4Mailboxes(IMAP4.IMAP4Client client, List<IMAP4.IMAP4Mailbox> mailboxes, string data)
        {
            using (StringReader reader = new StringReader(data))
            {
                Regex regex = new Regex(@"^[*=]\s+[^\s]+\s+\([^\)]*\)\s+(?:""(?<rn>[^""]+)""|(?<rn>[^\s]+))\s+(?:""(?<n>[^""]+)""|(?<n>[^\s]+))$", RegexOptions.IgnoreCase);
                while (reader.Peek() != -1)
                {
                    string line = reader.ReadLine();
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        mailboxes.Add(new Kingthy.Mail.IMAP4.IMAP4Mailbox(client, m.Groups["n"].Value));
                    }
                }
            }
        }
        #endregion
    }
}
