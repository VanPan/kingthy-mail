/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailPartMessage
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Net.Mime;
using System.IO;

namespace Kingthy.Mail
{
    /// <summary>
    /// 邮件的局部消息
    /// </summary>
    public class MailPartMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public MailPartMessage()
        {
            this.Headers = new MailHeaderCollection(this.OnAddHeader);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentType"></param>
        public MailPartMessage(string contentType) : this(new ContentType(contentType), TransferEncoding.SevenBit) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="contentTransferEncoding"></param>
        public MailPartMessage(ContentType contentType, TransferEncoding contentTransferEncoding) : this()
        {
            this.ContentType = contentType;
            this.ContentTransferEncoding = contentTransferEncoding;
        }
        /// <summary>
        /// Content-Type 头
        /// </summary>
        public ContentType ContentType { get; protected set; }
        /// <summary>
        /// Content-Transfer-Encoding 头
        /// </summary>
        public TransferEncoding ContentTransferEncoding { get; protected set; }
        /// <summary>
        /// 邮件头集合
        /// </summary>
        public MailHeaderCollection Headers { get; protected set; }
        /// <summary>
        /// 内容数据流
        /// </summary>
        public Stream ContentStream { get; internal set; }
        /// <summary>
        /// Message-Id 头
        /// </summary>
        public string MessageId { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected virtual void OnAddHeader(string name, string value)
        {
            switch (name.ToLowerInvariant())
            {
                case "message-id":
                    this.MessageId = value.Trim('<', '>', ' ');
                    break;
                case "content-type":
                    this.ContentType = new ContentType(value);
                    if (!string.IsNullOrEmpty(this.ContentType.Name))
                    {
                        this.ContentType.Name = Utility.DecodeHeaderValue(this.ContentType.Name);
                    }
                    break;
                case "content-transfer-encoding":
                    switch (value.ToLowerInvariant())
                    {
                        case "base64":
                            this.ContentTransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                            break;
                        case "quoted-printable":
                            this.ContentTransferEncoding = System.Net.Mime.TransferEncoding.QuotedPrintable;
                            break;
                        default:
                            this.ContentTransferEncoding = System.Net.Mime.TransferEncoding.SevenBit;
                            break;
                    }
                    break;
            }
        }
    }
}
