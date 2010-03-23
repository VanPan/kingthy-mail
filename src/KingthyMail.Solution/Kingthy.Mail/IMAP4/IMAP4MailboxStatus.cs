/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  IMAP4MailboxStatus
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace Kingthy.Mail.IMAP4
{
    /// <summary>
    /// 邮件的状态数据
    /// </summary>
    public class IMAP4MailboxStatus
    {
        /// <summary>
        /// 
        /// </summary>
        internal IMAP4MailboxStatus() { }

        /// <summary>
        /// 邮件消息总数
        /// </summary>
        public int TotalMessages { get; internal set; }

        /// <summary>
        /// 具有"\Recent"标记的邮件消息总数
        /// </summary>
        public int TotalRecentMessages { get; internal set; }

        /// <summary>
        /// 下一个邮件UID值
        /// </summary>
        public string NextUID { get; internal set; }

        /// <summary>
        /// 一个有效的邮件UID值
        /// </summary>
        public string ValidityUID { get; internal set; }

        /// <summary>
        /// 不具有"\Seen"标记的邮件消息总数
        /// </summary>
        public int TotalUnseenMessages { get; internal set; }

        /// <summary>
        /// 标记
        /// </summary>
        public string Flags { get; internal set; }
    }

    /// <summary>
    /// 邮箱状态的数据
    /// </summary>
    [Flags]
    public enum IMAP4MailboxStatusData
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0x0,
        /// <summary>
        /// 获取邮件消息总数
        /// </summary>
        MESSAGES = 0x01,
        /// <summary>
        /// 获取具有"\Recent"标记的邮件消息总数
        /// </summary>
        RECENT = 0x02,
        /// <summary>
        /// 获取下一个邮件UID值
        /// </summary>
        UIDNEXT = 0x04,
        /// <summary>
        /// 获取一个有效的邮件UID值
        /// </summary>
        UIDVALIDITY = 0x08,
        /// <summary>
        /// 获取不具有"\Seen"标记的邮件消息总数
        /// </summary>
        UNSEEN = 0x10,
        /// <summary>
        /// 获取所有状态数据
        /// </summary>
        All = MESSAGES | RECENT | UIDNEXT | UIDVALIDITY | UNSEEN
    }
}
