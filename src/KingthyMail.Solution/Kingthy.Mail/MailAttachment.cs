/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailAttachment
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mime;
using System.IO;

namespace Kingthy.Mail
{
    /// <summary>
    /// 邮件附件
    /// </summary>
    public class MailAttachment : MailPartMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public MailAttachment() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="partmessage"></param>
        public MailAttachment(MailPartMessage partmessage)
        {
            this.Headers = partmessage.Headers;
            this.ContentStream = partmessage.ContentStream;
            this.ContentTransferEncoding = partmessage.ContentTransferEncoding;
            this.ContentType = partmessage.ContentType;
            this.MessageId = partmessage.MessageId;

            this.ContentId = (this.Headers["content-id"] ?? "").Trim('<', '>', ' ');
            string cd = this.Headers["content-disposition"];
            if (!string.IsNullOrEmpty(cd))
            {
                this.ContentDisposition = new ContentDisposition(cd);
                if (!string.IsNullOrEmpty(this.ContentDisposition.FileName))
                {
                    this.ContentDisposition.FileName = Utility.DecodeHeaderValue(this.ContentDisposition.FileName);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string ContentId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ContentDisposition ContentDisposition { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void OnAddHeader(string name, string value)
        {
            switch (name.ToLowerInvariant())
            {
                case "content-id":
                    this.ContentId = value.Trim('<', '>', ' ');
                    break;
                case "content-disposition":
                    this.ContentDisposition = new ContentDisposition(value);
                    if (!string.IsNullOrEmpty(this.ContentDisposition.FileName))
                    {
                        this.ContentDisposition.FileName = Utility.DecodeHeaderValue(this.ContentDisposition.FileName);
                    }
                    break;
                default:
                    base.OnAddHeader(name, value);
                    break;
            }
        }

        /// <summary>
        /// 附件的文件名
        /// </summary>
        public string FileName
        {
            get
            {
                string fileName = this.ContentDisposition != null ? this.ContentDisposition.FileName : string.Empty;
                if (string.IsNullOrEmpty(fileName) && this.ContentType != null) fileName = this.ContentType.Name;
                return fileName;
            }
        }

        /// <summary>
        /// 将附件保存到某个目录下.文件名用附件的原名,如果没有原名则采用随机名称
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public void SaveToPath(string path)
        {
            string fileName = this.FileName;
            if (string.IsNullOrEmpty(fileName)) fileName = Path.GetRandomFileName();

            this.SaveToFile(Path.Combine(path, fileName));
        }
        /// <summary>
        /// 保存附件到某个文件上
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public void SaveToFile(string fileName)
        {
            if (this.ContentStream != null)
            {
                this.ContentStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    using (FileStream writer = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        if (this.ContentStream is MemoryStream)
                        {
                            ((MemoryStream)this.ContentStream).WriteTo(writer);
                        }
                        else
                        {
                            byte[] buffer = new byte[this.ContentStream.Length];
                            this.ContentStream.Read(buffer, 0, buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
                finally
                {
                    this.ContentStream.Seek(0, SeekOrigin.Begin);
                }
            }
        }
    }
}
