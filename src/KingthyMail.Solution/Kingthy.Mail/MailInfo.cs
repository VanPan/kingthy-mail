/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailInfo
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace Kingthy.Mail
{
    /// <summary>
    /// 邮件信息.包含邮件Id,和邮件在服务器的唯一Id值,和邮件大小
    /// </summary>
    public class MailInfo
    {
        /// <summary>
        /// 实例化邮件信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="uid"></param>
        /// <param name="size"></param>
        public MailInfo(int id, string uid, int size)
        {
            this.Id = id;
            this.UID = uid;
            this.Size = size;
        }
        /// <summary>
        /// 邮件Id
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// 邮件在服务器的唯一Id值
        /// </summary>
        public string UID { get; internal set; }

        /// <summary>
        /// 邮件大小
        /// </summary>
        public int Size { get; internal set; }
    }
}
