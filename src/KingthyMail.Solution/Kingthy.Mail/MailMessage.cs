/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailMessage
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;
using System.Collections.Specialized;

namespace Kingthy.Mail
{

    /// <summary>
    /// 邮件消息
    /// </summary>
    public class MailMessage : MailPartMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public MailMessage()
        {
            this.Bcc = new MailAddressCollection();
            this.CC = new MailAddressCollection();
            this.To = new MailAddressCollection();
            this.SubjectEncoding = Encoding.Default;
            this.AlternateViews = new List<MailPartMessage>();
            this.Attachments = new List<MailAttachment>();
        }
        /// <summary>
        /// 
        /// </summary>
        public MailAddressCollection Bcc { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public MailAddressCollection CC { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public MailAddressCollection To { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public MailAddress From { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public MailAddress Sender { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string Subject { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public Encoding SubjectEncoding { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public MailAddress ReplyTo { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime DeliveryDate { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public List<MailPartMessage> AlternateViews { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public List<MailAttachment> Attachments { get; private set; }

        /// <summary>
        /// 邮件正文内容.
        /// </summary>
        public string Body
        {
            get
            {
                Stream contentStream = this.ContentStream;
                ContentType contentType = this.ContentType;
                if (this.AlternateViews.Count != 0)
                {
                    contentType = null;
                    foreach (var av in this.AlternateViews)
                    {
                        if (av.ContentType != null && av.ContentType.MediaType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase))
                        {
                            contentType = av.ContentType;
                            contentStream = av.ContentStream;
                            break;
                        }
                    }
                    if (contentType == null)
                    {
                        contentType = this.AlternateViews[0].ContentType ?? this.ContentType;
                        contentStream = this.AlternateViews[0].ContentStream;
                    }
                }
                if (contentStream != null)
                {
                    try
                    {
                        contentStream.Seek(0, SeekOrigin.Begin);
                        StreamReader reader = new StreamReader(contentStream, Utility.GetEncoding(contentType.CharSet));
                        return reader.ReadToEnd();
                    }
                    finally
                    {
                        contentStream.Seek(0, SeekOrigin.Begin);
                    }
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// 获取某种类型的邮件正文内容
        /// </summary>
        /// <param name="contentType">内容类型:
        /// 空值       : 默认
        /// text/plain : 文本格式
        /// text/html : HTML格式
        /// </param>
        /// <returns></returns>
        public string GetBody(string contentType)
        {
            if (this.AlternateViews.Count == 0 || string.IsNullOrEmpty(contentType)) return this.Body;

            Stream cStream = null;
            ContentType cType = null;
            foreach (var av in this.AlternateViews)
            {
                if (av.ContentType != null && av.ContentType.MediaType.Equals(contentType, StringComparison.InvariantCultureIgnoreCase))
                {
                    cType = av.ContentType;
                    cStream = av.ContentStream;
                    break;
                }
            }
            if (cType != null)
            {
                try
                {
                    cStream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(cStream, Utility.GetEncoding(cType.CharSet));
                    return reader.ReadToEnd();
                }
                finally
                {
                    cStream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                return this.Body;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void OnAddHeader(string name, string value)
        {
            Encoding e;
            //解码
            string decodeValue = Utility.DecodeHeaderValue(value, out e);

            //处理已知的邮件头
            switch (name.ToLowerInvariant())
            {
                case "bcc":
                    Utility.AddMailAddresses(this.Bcc, decodeValue);
                    break;
                case "cc":
                    Utility.AddMailAddresses(this.CC, decodeValue);
                    break;
                case "from":
                    this.From = Utility.ToMailAddress(decodeValue);
                    break;
                case "sender":
                    this.Sender = Utility.ToMailAddress(decodeValue);
                    break;
                case "subject":
                    this.Subject = decodeValue;
                    this.SubjectEncoding = e;
                    break;
                case "reply-to":
                    this.ReplyTo = Utility.ToMailAddress(decodeValue);
                    break;
                case "to":
                    Utility.AddMailAddresses(this.To, decodeValue);
                    break;
                case "date":
                    this.DeliveryDate = Utility.ToDateTime(decodeValue);
                    break;
                default:
                    base.OnAddHeader(name, value);
                    break;
            }
        }
    }
}
