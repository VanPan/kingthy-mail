/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MIMEParser
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Net.Mime;

namespace Kingthy.Mail.Mime
{
    /// <summary>
    /// MIME 解析器
    /// </summary>
    public static class MimeParser
    {
        /// <summary>
        /// 从字符串中解析邮件内容
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MailMessage ParseFromString(string text)
        {
            return ParseFromString(text, Encoding.UTF8);
        }
        /// <summary>
        /// 从字符串中解析邮件内容
        /// </summary>
        /// <param name="text"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static MailMessage ParseFromString(string text, Encoding charset)
        {
            return Parse(charset.GetBytes(text), charset);
        }
        /// <summary>
        /// 从字符串中解析邮件内容
        /// </summary>
        /// <param name="data"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static MailMessage Parse(byte[] data, Encoding charset)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                return Parse(stream, charset);
            }
        }
        /// <summary>
        /// 根据文件解析邮件内容
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static MailMessage Parse(string fileName)
        {
            return Parse(fileName, Encoding.UTF8);
        }
        /// <summary>
        /// 根据文件解析邮件内容
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="charset">默认编码</param>
        /// <returns></returns>
        public static MailMessage Parse(string fileName, Encoding charset)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return Parse(new MailStreamReader(stream), charset);
            }
        }
        /// <summary>
        /// 根据数据流解析邮件内容
        /// </summary>
        /// <param name="stream">数据流</param>
        /// <param name="charset">默认编码</param>
        /// <returns></returns>
        public static MailMessage Parse(Stream stream, Encoding charset)
        {
            return Parse(new MailStreamReader(stream), charset);
        }
        /// <summary>
        /// 邮件邮件内容
        /// </summary>
        /// <param name="mailReader">邮件流读取器</param>
        /// <param name="charset">默认编码</param>
        /// <returns></returns>
        public static MailMessage Parse(MailStreamReader mailReader, Encoding charset)
        {
            bool isSectionOver = false;
            MailMessage mail = ParseMessage(mailReader, charset, null, ref isSectionOver);
            return mail;
        }
        /// <summary>
        /// 邮件邮件内容
        /// </summary>
        /// <param name="mailReader"></param>
        /// <param name="charset"></param>
        /// <param name="boundary"></param>
        /// <param name="isSectionOver"></param>
        /// <returns></returns>
        private static MailMessage ParseMessage(MailStreamReader mailReader, Encoding charset, string boundary, ref bool isSectionOver)
        {
            isSectionOver = false;
            MailMessage mail = new MailMessage();
            ParseMimeHeader(mailReader, charset, mail);
            if (mail.Headers.Count == 0) return null;
            if (mail.ContentType != null && !string.IsNullOrEmpty(mail.ContentType.CharSet))
            {
                charset = Utility.GetEncoding(mail.ContentType.CharSet);
            }

            ParseMimeContent(mailReader, charset, mail, boundary, ref isSectionOver);           
            
            return mail;
        }
        /// <summary>
        /// 解析邮件头
        /// </summary>
        /// <param name="mailReader"></param>
        /// <param name="charset"></param>
        /// <param name="mail"></param>
        private static void ParseMimeHeader(MailStreamReader mailReader, Encoding charset, MailMessage mail)
        {
            string line = string.Empty;
            string headName = string.Empty;
            string headValue = string.Empty;
            //邮件头是碰到空行结束
            while (!string.IsNullOrEmpty(line = mailReader.ReadLineAsString(charset)))
            {
                if (char.IsWhiteSpace(line[0]))
                {
                    //行与空白字符开头则是保持上一个head
                    headValue += line.TrimStart();
                }
                else
                {
                    if (headName.Length > 0)
                    {
                        mail.Headers.Add(headName, headValue);
                        headName = string.Empty;
                    }

                    int p = line.IndexOf(':');
                    if (p != -1)
                    {
                        headName = line.Substring(0, p).Trim();
                        headValue = p < line.Length - 1 ? line.Substring(p + 1).Trim() : string.Empty;
                    }
                }
            }
            if (headName.Length > 0)
            {
                mail.Headers.Add(headName, headValue);
            }
        }
        /// <summary>
        /// 解析邮件正文内容
        /// </summary>
        /// <param name="mailReader"></param>
        /// <param name="charset"></param>
        /// <param name="mail"></param>
        /// <param name="terminativeLine"></param>
        /// <param name="isSectionOver"></param>
        private static void ParseMimeContent(MailStreamReader mailReader, Encoding charset, MailMessage mail, string terminativeLine, ref bool isSectionOver)
        {
            if (!string.IsNullOrEmpty(mail.ContentType.Boundary))
            {
                //有正文分界线定义.则读取正文分段数据
                string boundary = "--" + mail.ContentType.Boundary;
                string line = null;

                //跳过分界线
                while (!string.IsNullOrEmpty(line = mailReader.ReadLineAsString(charset)))
                {
                    if (line == boundary) break;
                }

                //循环读取分段内容
                do
                {
                    MailMessage partMail = ParseMessage(mailReader, charset, boundary, ref isSectionOver);
                    if (partMail != null)
                    {
                        switch (partMail.ContentType.MediaType.ToLowerInvariant())
                        {
                            case "multipart/alternative":
                            case "multipart/related":
                            case "multipart/mixed":
                                foreach (MailPartMessage item in partMail.AlternateViews) mail.AlternateViews.Add(item);
                                foreach (MailAttachment item in partMail.Attachments) mail.Attachments.Add(item);
                                break;
                            case "text/html":
                            case "text/plain":
                                if (partMail.ContentStream != null)
                                {
                                    mail.AlternateViews.Add(partMail);
                                }
                                break;
                            default:
                                if (partMail.ContentStream != null)
                                {
                                    mail.Attachments.Add(new MailAttachment(partMail));
                                }
                                break;
                        }
                    }
                    //块已结束.则用顶级的分隔线
                    if (isSectionOver) boundary = terminativeLine;

                } while (!mailReader.EOS);
            }
            else
            {
                //没有正文分界线定义.所以将后面的内容都看作是邮件正文
                mail.ContentStream = ReadMailBodyStream(mailReader, charset, mail.ContentType, mail.ContentTransferEncoding, terminativeLine, ref isSectionOver);
            }
        }

        /// <summary>
        /// 读取邮件正文的数据流
        /// </summary>
        /// <param name="mailReader"></param>
        /// <param name="charset"></param>
        /// <param name="contentType"></param>
        /// <param name="transferEncoding"></param>
        /// <param name="terminativeLine"></param>
        /// <param name="isSectionOver"></param>
        /// <returns></returns>
        private static Stream ReadMailBodyStream(MailStreamReader mailReader, Encoding charset, ContentType contentType, TransferEncoding transferEncoding, string terminativeLine, ref bool isSectionOver)
        {
            StringBuilder buffer = new StringBuilder();
            string line = null;

            string terminativeLine2 = terminativeLine == null ? null : terminativeLine + "--";
            isSectionOver = false;
            while ((line = mailReader.ReadLineAsString(charset)) != null && line != terminativeLine)
            {
                if (line == terminativeLine2)
                {
                    isSectionOver = true;
                    break;
                }
                buffer.AppendLine(line);
            }
            string text = buffer.ToString();
            MemoryStream stream = new MemoryStream();
            if (!contentType.MediaType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase) 
                && transferEncoding == TransferEncoding.Base64)
            {
                //如果内容是非文本格式.并且是base64编码则为二进制内容
                byte[] data = Convert.FromBase64String(text);
                stream.Write(data, 0, data.Length);
            }
            else
            {
                StreamWriter writer = new StreamWriter(stream, charset);
                writer.Write(Utility.DecodeMimeText(text, transferEncoding, charset));
                writer.Flush();

            }
            return stream;
        }
    }
}
