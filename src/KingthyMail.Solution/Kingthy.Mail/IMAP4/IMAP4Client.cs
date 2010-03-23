/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  IMAP4Client
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net.Security;
using System.Net.Mime;
using Kingthy.Mail.Mime;
using System.Text.RegularExpressions;

namespace Kingthy.Mail.IMAP4
{
    /// <summary>
    /// 采用IMAP4协议接收处理邮件
    /// </summary>
    public class IMAP4Client : IDisposable
    {
        #region 构造函数
        /// <summary>
        /// 初始化IMAP4Client实例
        /// </summary>
        /// <param name="host">IMAP4邮件服务器的主机名或IP地址</param>
        /// <param name="username">登录IMAP4邮件服务器的用户名</param>
        /// <param name="password">登录IMAP4邮件服务器的用户密码</param>
        public IMAP4Client(string host, string username, string password) : this(host, 143, username, password) { }
        /// <summary>
        /// 初始化IMAP4Client实例
        /// </summary>
        /// <param name="host">IMAP4邮件服务器的主机名或IP地址</param>
        /// <param name="port">IMAP4邮件服务器的端口</param>
        /// <param name="username">登录IMAP4邮件服务器的用户名</param>
        /// <param name="password">登录IMAP4邮件服务器的用户密码</param>
        public IMAP4Client(string host, int port, string username, string password) : this(host, port, username, password, false) { }
        /// <summary>
        /// 初始化IMAP4Client实例
        /// </summary>
        /// <param name="host">IMAP4邮件服务器的主机名或IP地址</param>
        /// <param name="port">IMAP4邮件服务器的端口</param>
        /// <param name="username">登录IMAP4邮件服务器的用户名</param>
        /// <param name="password">登录IMAP4邮件服务器的用户密码</param>
        /// <param name="enableSsl">是否使用安全套接字层 (SSL) 加密连接。</param>
        public IMAP4Client(string host, int port, string username, string password, bool enableSsl)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
            this.EnableSsl = enableSsl;
            this.Timeout = 60000;
            this.IsDebugMode = false;
            this.LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imap4trace.log");
            this.connectionState = IMAP4ClientState.Disconnected;
            this.Charset = Encoding.ASCII;
        }
        #endregion

        #region 属性定义
        /// <summary>
        /// IMAP4邮件服务器的主机名或IP地址
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// IMAP4邮件服务器的连接端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 登录IMAP4邮件服务器的用户名
        /// </summary>
        public string Username { get; private set; }
        /// <summary>
        /// 登录IMAP4邮件服务器的用户密码
        /// </summary>
        public string Password { get; private set; }
        /// <summary>
        /// 是否使用安全套接字层 (SSL) 加密连接。
        /// </summary>
        public bool EnableSsl { get; private set; }

        /// <summary>
        /// 获取或设置一个值，该值指定发送或接收网络数据时的超时时间。单位:毫秒
        /// </summary>
        /// <remarks>该值默认为60,000,也即是60秒</remarks>
        public int Timeout
        {
            get
            {
                return this._Timeout;
            }
            set
            {
                this._Timeout = value;
                if (this.connectionStream != null && this.connectionState == IMAP4ClientState.Connected)
                {
                    this.connectionStream.ReadTimeout = this.connectionStream.WriteTimeout = value;
                }
            }
        }
        /// <summary>
        /// 超时时间
        /// </summary>
        private int _Timeout;

        /// <summary>
        /// 是否是调试模式,如果是则会在日志文件里记录与服务器通信交互的数据
        /// </summary>
        public bool IsDebugMode { get; set; }

        /// <summary>
        /// 日志文件
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// 编码时采用的字符集.默认为Ascii
        /// </summary>
        public Encoding Charset { get; set; }

        /// <summary>
        /// 标识符的数字
        /// </summary>
        private int IdentifierNumber = 1;
        #endregion

        #region 网络连接
        /// <summary>
        /// IMAP4Client的连接状态
        /// </summary>
        internal enum IMAP4ClientState
        {
            /// <summary>
            /// 未知
            /// </summary>
            None = 0,
            /// <summary>
            /// 未连接或已关闭
            /// </summary>
            Disconnected,
            /// <summary>
            /// 已授权.即已完成用户帐号验证
            /// </summary>
            Authorization,
            /// <summary>
            /// 已连接.
            /// </summary>
            Connected
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        private IMAP4ClientState connectionState;

        /// <summary>
        /// TCP连接
        /// </summary>
        private TcpClient connection;

        /// <summary>
        /// 连接流
        /// </summary>
        private Stream connectionStream;

        /// <summary>
        /// 连接流读取对象
        /// </summary>
        private MailStreamReader connectionStreamReader;

        /// <summary>
        /// 跟踪数据
        /// </summary>
        /// <param name="data"></param>
        private void TraceData(string data)
        {
            if (this.IsDebugMode)
            {
                try
                {
                    File.AppendAllText(this.LogFile, string.Concat(data, "\r\n"));
                }
                catch { }
            }
        }

        /// <summary>
        /// 读取执行命令后服务器返回的数据.
        /// </summary>
        /// <param name="commandId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal bool ReadCommandReturnData(string commandId, out string data)
        {
            StringBuilder buffer = new StringBuilder();
            string result = string.Empty;
            string text = string.Empty;
            bool flag = false;
            try
            {
                do
                {
                    flag = false;
                    text = connectionStreamReader.ReadLineAsString(this.Charset);
                    if (text == null) break;
                    string[] d = text.Split(new char[] { ' ' }, 3);
                    if (d.Length > 1)
                    {
                        flag = string.IsNullOrEmpty(commandId) ? true : commandId.Equals(d[0], StringComparison.InvariantCultureIgnoreCase);
                        if ("*" == d[0] || "+" == d[0] || flag)
                        {
                            result = d[1];
                            if (flag) break;                            
                        }
                    }
                    buffer.AppendLine(text);
                    
                } while (true);
            }
            catch { }

            data = buffer.ToString();
            if (flag && text != null) buffer.AppendLine(text);

            TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), buffer.ToString()));
            return "ok".Equals(result, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 获取命令的标识符
        /// </summary>
        /// <returns></returns>
        private string GetCommandIdentifiers()
        {
            return string.Concat("C", (IdentifierNumber++).ToString("0000"));
        }

        /// <summary>
        /// 发送命令到服务器
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal bool SendCommand(string command)
        {
            string data;
            return this.SendCommand(command, out data);
        }
        /// <summary>
        /// 发送命令到服务器
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal bool SendCommand(string command, out string data)
        {
            string commandId = GetCommandIdentifiers();
            command = commandId + " " + command;

            TraceData(string.Format("{0} 发送: {1}", DateTime.Now.ToString("HH:mm:ss"), command));

            data = null;
            byte[] buffer = Encoding.ASCII.GetBytes(command + System.Environment.NewLine);
            try
            {
                this.connectionStream.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
            this.connectionStream.Flush();
            return this.ReadCommandReturnData(commandId, out data);
        }

        /// <summary>
        /// 确认连接状态
        /// </summary>
        /// <param name="state"></param>
        internal void EnsureState(IMAP4ClientState state)
        {
            if (this.connectionState != state)
                throw new IMAP4Exception(string.Format("此操作需要连接状态为:{0},但当前连接状态为:{1}", state, this.connectionState));
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            if (this.connectionState != IMAP4ClientState.Disconnected)
            {
                //不允许重复连接
                return;
            }
            TraceData(string.Format("{0} 正在连接服务器 {1}:{2}......", DateTime.Now.ToString("HH:mm:ss"), this.Host, this.Port));
            try
            {
                this.connection = new TcpClient(this.Host, this.Port);
                this.connection.SendTimeout = this.connection.ReceiveTimeout = this.Timeout;
            }
            catch (Exception ex)
            {
                throw new IMAP4Exception(string.Format("连接服务器{0}:{1}时失败.错误消息:{2}", this.Host, this.Port, ex.Message), ex);
            }

            //获取连接流
            try
            {
                this.connectionStream = this.connection.GetStream();
            }
            catch (Exception ex)
            {
                throw new IMAP4Exception(string.Format("连接服务器{0}成功,但获取网络流时失败.错误消息:{1}", this.Host, ex.Message), ex);
            }

            if (this.EnableSsl)
            {
                //采用SSL安全连接
                try
                {
                    this.connectionStream = new SslStream(this.connectionStream, false);
                }
                catch (Exception ex)
                {
                    throw new IMAP4Exception(string.Format("连接服务器{0}成功,但获取SSL交互数据时失败.错误消息:{1}", this.Host, ex.Message), ex);
                }
                try
                {
                    ((SslStream)this.connectionStream).AuthenticateAsClient(this.Host);
                }
                catch (Exception ex)
                {
                    throw new IMAP4Exception(string.Format("连接服务器{0}成功,但初始化SSL身份验证时失败.错误消息:{1}", this.Host, ex.Message), ex);
                }
            }
            this.connectionStreamReader = new MailStreamReader(this.connectionStream);

            string data;
            if (!this.ReadCommandReturnData(null, out data)) throw new IMAP4Exception(string.Format("已连接服务器到{0},但未获取到服务器的欢迎信息.", this.Host));

            //进入认证状态
            this.connectionState = IMAP4ClientState.Authorization;
            if (!this.SendCommand(string.Format("LOGIN {0} {1}", this.Username, this.Password), out data)) throw new IMAP4Exception(string.Format("服务器{0}拒绝帐号{1}的登录.返回消息:{2}.", this.Host, this.Username, data));

            //进入连接状态
            this.connectionState = IMAP4ClientState.Connected;
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Disconnect()
        {
            if (this.connectionState != IMAP4ClientState.Disconnected)
            {
                try
                {
                    this.SendCommand("LOGOUT");                    
                }
                finally
                {
                    if (this.connectionStream != null) this.connectionStream.Close();
                    if (this.connectionStreamReader != null) this.connectionStreamReader.Close();
                    this.connection.Close();
                    this.connectionStream = null;
                }
                this.connectionState = IMAP4ClientState.Disconnected;
                TraceData(string.Format("{0} 已和服务器 {1} 断开连接.", DateTime.Now.ToString("HH:mm:ss"), this.Host));
            }
        }
        #endregion

        #region IMAP4相关命令封装
        /// <summary>
        /// 建立邮箱
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IMAP4Mailbox CreateMailbox(string name)
        {
            this.EnsureState(IMAP4ClientState.Connected);

            if (this.SendCommand("CREATE " + name))
            {
                return new IMAP4Mailbox(this, name);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 选择某个邮箱
        /// </summary>
        /// <param name="name"></param>
        /// <param name="status">返回邮箱的状态信息</param>
        /// <returns></returns>
        public IMAP4Mailbox SelectMailbox(string name, out IMAP4MailboxStatus status)
        {
            this.EnsureState(IMAP4ClientState.Connected);

            status = new IMAP4MailboxStatus();
            string data;
            if (this.SendCommand("SELECT " + name, out data))
            {
                Utility.ParseIMAP4MailStatus(status, data);
                return new IMAP4Mailbox(this, name) { IsSelected = true };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 关闭当前邮箱
        /// </summary>
        public void CloseCurrentMailbox()
        {
            this.EnsureState(IMAP4ClientState.Connected);

            this.SendCommand("CLOSE");
        }
        /// <summary>
        /// 获取所有邮箱
        /// </summary>
        /// <returns></returns>
        public List<IMAP4Mailbox> GetAllMailboxes()
        {
            return this.ListMailboxes("", "*");
        }
        /// <summary>
        /// 列举所有邮箱
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="mailboxNamePattern"></param>
        /// <returns></returns>
        public List<IMAP4Mailbox> ListMailboxes(string referenceName, string mailboxNamePattern)
        {
            this.EnsureState(IMAP4ClientState.Connected);

            List<IMAP4Mailbox> mailboxes = new List<IMAP4Mailbox>();
            string data;
            if (this.SendCommand(string.Format("LIST \"{0}\" \"{1}\"", referenceName, mailboxNamePattern), out data))
            {
                Utility.ParseIMAP4Mailboxes(this, mailboxes, data);
            }
            return mailboxes;
        }

        /// <summary>
        /// 列举所有已订阅的邮箱
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="mailboxNamePattern"></param>
        /// <returns></returns>
        public List<IMAP4Mailbox> ListSubscribedMailboxes(string referenceName, string mailboxNamePattern)
        {
            this.EnsureState(IMAP4ClientState.Connected);

            List<IMAP4Mailbox> mailboxes = new List<IMAP4Mailbox>();
            string data;
            if (this.SendCommand(string.Format("LSUB \"{0}\" \"{1}\"", referenceName, mailboxNamePattern), out data))
            {
                Utility.ParseIMAP4Mailboxes(this, mailboxes, data);
            }
            return mailboxes;
        }
        #endregion

        #region IDisposable 成员
        /// <summary>
        /// 释放内存
        /// </summary>
        public void Dispose()
        {
            this.Disconnect();
        }

        #endregion
    }
}
