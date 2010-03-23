/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  IMAP4Mailbox
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Kingthy.Mail.IMAP4
{
    /// <summary>
    /// 邮箱
    /// </summary>
    public class IMAP4Mailbox
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        internal IMAP4Mailbox(IMAP4Client client, string name)
        {
            this.Client = client;
            this.Name = name;
            this.IsDeleted = false;
            this.IsSubscribed = false;
            this.IsSelected = false;
        }
        /// <summary>
        /// 
        /// </summary>
        private IMAP4Client Client;
        /// <summary>
        /// 邮箱名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 是否已删除
        /// </summary>
        public bool IsDeleted { get; internal set; }

        /// <summary>
        /// 是否已订阅
        /// </summary>
        public bool IsSubscribed { get; internal set; }

        /// <summary>
        /// 是否已选择
        /// </summary>
        public bool IsSelected { get; internal set; }

        #region IMAP4命令封装
        /// <summary>
        /// 更改邮箱名称
        /// </summary>
        /// <param name="name">新的名称</param>
        /// <returns></returns>
        public bool Rename(string name)
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            bool f = this.Client.SendCommand(string.Format("RENAME {0} {1}", this.Name, name));
            if (f) this.Name = name;

            return f;
        }

        /// <summary>
        /// 删除邮箱
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            this.IsDeleted = this.Client.SendCommand(string.Format("DELETE {0}", this.Name));

            return this.IsDeleted;
        }

        /// <summary>
        /// 订阅邮箱
        /// </summary>
        /// <returns></returns>
        public bool Subscribe()
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            this.IsSubscribed = this.Client.SendCommand(string.Format("SUBSCRIBE {0}", this.Name));

            return this.IsSubscribed;
        }

        /// <summary>
        /// 取消订阅邮箱
        /// </summary>
        /// <returns></returns>
        public bool UnSubscribe()
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            bool f = this.Client.SendCommand(string.Format("UNSUBSCRIBE {0}", this.Name));
            if (f)
            {
                this.IsSubscribed = false;
            }

            return f;
        }

        /// <summary>
        /// 选择此邮箱
        /// </summary>
        /// <returns></returns>
        public IMAP4MailboxStatus Select()
        {
            IMAP4MailboxStatus status = null;

            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);
            string data;
            bool f = this.Client.SendCommand(string.Format("SELECT {0}", this.Name), out data);
            if (f)
            {
                status = new IMAP4MailboxStatus();
                Utility.ParseIMAP4MailStatus(status, data);
                this.IsSelected = true;
            }

            return status;
        }

        /// <summary>
        /// 关闭此邮箱,停止对此邮箱的选择
        /// </summary>
        public void Close()
        {
            if (this.IsSelected)
            {
                this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

                bool f = this.Client.SendCommand("CLOSE");
                if (f)
                {
                    this.IsSelected = false;
                }
            }
        }
        /// <summary>
        /// 检查邮箱的状态
        /// </summary>
        /// <returns></returns>
        public IMAP4MailboxStatus Examine()
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            IMAP4MailboxStatus status = null;
            string data;
            if (this.Client.SendCommand("EXAMINE " + this.Name, out data))
            {
                status = new IMAP4MailboxStatus();
                Utility.ParseIMAP4MailStatus(status, data);
            }
            return status;
        }
        /// <summary>
        /// 获取邮箱的状态
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IMAP4MailboxStatus GetStatus(IMAP4MailboxStatusData data)
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            IMAP4MailboxStatus status = new IMAP4MailboxStatus();

            List<string> items = new List<string>();
            if ((data & IMAP4MailboxStatusData.MESSAGES) == IMAP4MailboxStatusData.MESSAGES) items.Add("MESSAGES");
            if ((data & IMAP4MailboxStatusData.RECENT) == IMAP4MailboxStatusData.RECENT) items.Add("RECENT");
            if ((data & IMAP4MailboxStatusData.UIDNEXT) == IMAP4MailboxStatusData.UIDNEXT) items.Add("UIDNEXT");
            if ((data & IMAP4MailboxStatusData.UIDVALIDITY) == IMAP4MailboxStatusData.UIDVALIDITY) items.Add("UIDVALIDITY");
            if ((data & IMAP4MailboxStatusData.UNSEEN) == IMAP4MailboxStatusData.UNSEEN) items.Add("UNSEEN");

            string output;
            if (this.Client.SendCommand(string.Format("STATUS {0} ({1})", this.Name, string.Join(" ", items.ToArray())), out output))
            {
                using (StringReader reader = new StringReader(output))
                {
                    Regex regex = new Regex(@"^[^ ]+ STATUS [^ ]+ \(((?<name>[A-Za-z]+) (?<val>[^ \)]+)\s*)+\)$");
                    while (reader.Peek() != -1)
                    {
                        string text = reader.ReadLine();
                        if (!string.IsNullOrEmpty(text))
                        {
                            Match m = regex.Match(text);
                            if (m.Success)
                            {
                                var names = m.Groups["name"].Captures;
                                var values = m.Groups["val"].Captures;
                                for (var i = 0; i < names.Count; i++)
                                {
                                    string name = names[i].Value.ToUpper();
                                    switch (name)
                                    {
                                        case "MESSAGES":
                                            status.TotalMessages = Utility.ToInt32(values[i].Value);
                                            break;
                                        case "RECENT":
                                            status.TotalRecentMessages = Utility.ToInt32(values[i].Value);
                                            break;
                                        case "UIDNEXT":
                                            status.NextUID = values[i].Value;
                                            break;
                                        case "UIDVALIDITY":
                                            status.ValidityUID = values[i].Value;
                                            break;
                                        case "UNSEEN":
                                            status.TotalUnseenMessages = Utility.ToInt32(values[i].Value);
                                            break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            return status;
        }

        /// <summary>
        /// 获取邮箱下的所有邮件
        /// </summary>
        /// <param name="allData">是否获取邮件的所有内容,如果不是则只获取邮件头部,否则包括邮件的内容主体与附件</param>
        /// <returns></returns>
        public List<MailMessage> GetMailMessages(bool allData)
        {
            List<MailMessage> mails = new List<MailMessage>();
            string data = FetchMailData("1:*", string.Format("(BODY[{0}])", allData ? "" : "HEADER"));
            if (!string.IsNullOrEmpty(data))
            {
                Utility.ParseIMAP4MailMessages(mails, data);
            }
            return mails;
        }

        /// <summary>
        /// 获取某封邮件
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public MailMessage GetMailMessage(int mailId)
        {
            List<MailMessage> mails = new List<MailMessage>();
            string data = FetchMailData(mailId.ToString(), "(BODY[])");
            if (!string.IsNullOrEmpty(data))
            {
                Utility.ParseIMAP4MailMessages(mails, data);
            }
            return mails.Count > 0 ? mails[0] : null;
        }

        /// <summary>
        /// 获取某封邮件
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public string GetMailContent(int mailId)
        {
            List<string> contents = new List<string>();
            string data = FetchMailData(mailId.ToString(), "(BODY[])");
            if (!string.IsNullOrEmpty(data))
            {
                contents = Utility.ParseIMAP4MailContents(data);
            }
            return contents.Count > 0 ? contents[0] : null;
        }
        /// <summary>
        /// 下载某封邮件并且保存到某文件
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="fileName"></param>
        public bool DownloadMail(int mailId, string fileName)
        {
            string content = this.GetMailContent(mailId);
            if (!string.IsNullOrEmpty(content))
            {
                File.WriteAllText(fileName, content, Encoding.UTF8);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 获取某封邮件的大小
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public int GetMailSize(int mailId)
        {
            int size = -1;
            string data = FetchMailData(mailId.ToString(), "(RFC822.SIZE)");
            if (!string.IsNullOrEmpty(data))
            {
                string text;
                if (Utility.FindMatchItem(data, @"RFC822.SIZE (\d+)", RegexOptions.IgnoreCase, out text))
                {
                    size = Utility.ToInt32(text);
                }
            }
            return size;
        }
        /// <summary>
        /// 获取邮件数据
        /// </summary>
        /// <param name="sequence">邮件序号,可以为单个数字,如:"1"--表示获取序号为1的邮件数据;也可以为一个数字范围:"1:4"--表示获取序号为1到4的邮件数据,"1:*"--表示获取从序号1开始的所有邮件数据</param>
        /// <param name="items">要获取的邮件内容项</param>
        /// <returns></returns>
        public string FetchMailData(string sequence, string items)
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            string data = null;
            this.Client.SendCommand(string.Format("FETCH {0} {1}", sequence, items), out data);
            return data;
        }

        /// <summary>
        /// 增加邮件的标记
        /// </summary>
        /// <param name="sequence">邮件序号,可以为单个数字,如:"1"--表示获取序号为1的邮件数据;也可以为一个数字范围:"1:4"--表示获取序号为1到4的邮件数据,"1:*"--表示获取从序号1开始的所有邮件数据</param>
        /// <param name="flags">要增加的标记列,如"\Deleted","\Seen"或多个标记的组合,如:"\Deleted \Seen"</param>
        /// <returns></returns>
        public bool AddMailFlags(string sequence, string flags)
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            return this.Client.SendCommand(string.Format("STORE {0} +FLAGS ({1})", sequence, flags));
        }
        /// <summary>
        /// 移除邮件的标记
        /// </summary>
        /// <param name="sequence">邮件序号,可以为单个数字,如:"1"--表示获取序号为1的邮件数据;也可以为一个数字范围:"1:4"--表示获取序号为1到4的邮件数据,"1:*"--表示获取从序号1开始的所有邮件数据</param>
        /// <param name="flags">要移除的标记列,如"\Deleted","\Seen"或多个标记的组合,如:"\Deleted \Seen"</param>
        /// <returns></returns>
        public bool RemoveMailFlags(string sequence, string flags)
        {
            this.Client.EnsureState(IMAP4Client.IMAP4ClientState.Connected);

            return this.Client.SendCommand(string.Format("STORE {0} -FLAGS ({1})", sequence, flags));
        }

        /// <summary>
        /// 删除服务器上的邮件,删除后无法恢复
        /// </summary>
        /// <param name="sequence">邮件序号,可以为单个数字,如:"1"--表示获取序号为1的邮件数据;也可以为一个数字范围:"1:4"--表示获取序号为1到4的邮件数据,"1:*"--表示获取从序号1开始的所有邮件数据</param>
        /// <returns></returns>
        public bool RemoveMails(string sequence)
        {
            if (this.AddMailFlags(sequence, @"\Deleted"))
            {
                return this.Client.SendCommand("EXPUNGE");
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
