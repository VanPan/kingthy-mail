/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  POP3Client
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

namespace Kingthy.Mail.POP3
{
    /// <summary>
    /// 采用POP3协议接收处理邮件
    /// </summary>
    public class POP3Client : IDisposable
    {
        #region 构造函数
        /// <summary>
        /// 初始化POP3Client实例
        /// </summary>
        /// <param name="host">POP3邮件服务器的主机名或IP地址</param>
        /// <param name="username">登录POP3邮件服务器的用户名</param>
        /// <param name="password">登录POP3邮件服务器的用户密码</param>
        public POP3Client(string host, string username, string password) : this(host, 110, username, password) { }
        /// <summary>
        /// 初始化POP3Client实例
        /// </summary>
        /// <param name="host">POP3邮件服务器的主机名或IP地址</param>
        /// <param name="port">POP3邮件服务器的端口</param>
        /// <param name="username">登录POP3邮件服务器的用户名</param>
        /// <param name="password">登录POP3邮件服务器的用户密码</param>
        public POP3Client(string host, int port, string username, string password) : this(host, port, username, password, false) { }
        /// <summary>
        /// 初始化POP3Client实例
        /// </summary>
        /// <param name="host">POP3邮件服务器的主机名或IP地址</param>
        /// <param name="port">POP3邮件服务器的端口</param>
        /// <param name="username">登录POP3邮件服务器的用户名</param>
        /// <param name="password">登录POP3邮件服务器的用户密码</param>
        /// <param name="enableSsl">是否使用安全套接字层 (SSL) 加密连接。</param>
        public POP3Client(string host, int port, string username, string password, bool enableSsl)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
            this.EnableSsl = enableSsl;
            this.Timeout = 60000;
            this.IsDebugMode = false;
            this.LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pop3trace.log");
            this.connectionState = POP3ClientState.Disconnected;
            this.Charset = Encoding.ASCII;
        }
        #endregion

        #region 属性定义
        /// <summary>
        /// POP3邮件服务器的主机名或IP地址
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// POP3邮件服务器的连接端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 登录POP3邮件服务器的用户名
        /// </summary>
        public string Username { get; private set; }
        /// <summary>
        /// 登录POP3邮件服务器的用户密码
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
                if (this.connectionStream != null && this.connectionState == POP3ClientState.Connected)
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
        #endregion

        #region 网络连接
        /// <summary>
        /// POP3Client的连接状态
        /// </summary>
        private enum POP3ClientState
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
            /// 已授权.即已完成USER和PASS验证
            /// </summary>
            Authorization,
            /// <summary>
            /// 已连接.即服务器已完成对邮件消息的锁定状态
            /// </summary>
            Connected
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        private POP3ClientState connectionState;

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
        /// 读取服务器返回的单行数据.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ReadSingleLineData(out string data)
        {
            data = null;
            try
            {
                data = connectionStreamReader.ReadLineAsString(this.Charset);
            }
            catch { }

            if (data != null) TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), data));
            return !string.IsNullOrEmpty(data) && data[0] == '+';
        }
        /// <summary>
        /// 读取服务器返回的多行数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ReadMultiLineData(out string data)
        {
            data = null;
            try
            {
                data = connectionStreamReader.ReadLineAsString(this.Charset);
            }
            catch { }
            if (data != null) TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), data));
            if (data != null)
            {
                if (data.Length > 0 && data[0] == '.')
                {
                    //已接受到单行的 "."数据.所以已结束多行的数据流
                    if (data.Length == 1) return false;

                    data = data.Substring(1);
                }
            }
            return data != null;
        }
        /// <summary>
        /// 发送命令到服务器
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool SendCommand(string command, out string data)
        {
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
            return this.ReadSingleLineData(out data);
        }

        /// <summary>
        /// 确认连接状态
        /// </summary>
        /// <param name="state"></param>
        private void EnsureState(POP3ClientState state)
        {
            if (this.connectionState != state)
                throw new POP3Exception(string.Format("此操作需要连接状态为:{0},但当前连接状态为:{1}", state, this.connectionState));
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            if (this.connectionState != POP3ClientState.Disconnected)
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
                throw new POP3Exception(string.Format("连接服务器{0}:{1}时失败.错误消息:{2}", this.Host, this.Port, ex.Message), ex);
            }

            //获取连接流
            try
            {
                this.connectionStream = this.connection.GetStream();
            }
            catch (Exception ex)
            {
                throw new POP3Exception(string.Format("连接服务器{0}成功,但获取网络流时失败.错误消息:{1}", this.Host, ex.Message), ex);
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
                    throw new POP3Exception(string.Format("连接服务器{0}成功,但获取SSL交互数据时失败.错误消息:{1}", this.Host, ex.Message), ex);
                }
                try
                {
                    ((SslStream)this.connectionStream).AuthenticateAsClient(this.Host);
                }
                catch (Exception ex)
                {
                    throw new POP3Exception(string.Format("连接服务器{0}成功,但初始化SSL身份验证时失败.错误消息:{1}", this.Host, ex.Message), ex);
                }
            }
            this.connectionStreamReader = new MailStreamReader(this.connectionStream);

            string data;
            if (!this.ReadSingleLineData(out data)) throw new POP3Exception(string.Format("已连接服务器到{0},但未获取到服务器的欢迎信息.", this.Host));

            //进入认证状态
            this.connectionState = POP3ClientState.Authorization;
            if (!this.SendCommand("USER " + this.Username, out data)) throw new POP3Exception(string.Format("服务器{0}拒绝帐号{1}的登录.返回消息:{2}.", this.Host, this.Username, data));
            if (!this.SendCommand("PASS " + this.Password, out data)) throw new POP3Exception(string.Format("服务器拒绝用户登录,用户{1}的密码有误.返回消息:{2}.", this.Host, this.Username, data));

            //进入连接状态
            this.connectionState = POP3ClientState.Connected;
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Disconnect()
        {
            if (this.connectionState != POP3ClientState.Disconnected)
            {
                string data;
                try
                {
                    this.SendCommand("QUIT", out data);                    
                }
                finally
                {
                    if (this.connectionStream != null) this.connectionStream.Close();
                    if (this.connectionStreamReader != null) this.connectionStreamReader.Close();
                    this.connection.Close();
                    this.connectionStream = null;
                }
                this.connectionState = POP3ClientState.Disconnected;
                TraceData(string.Format("{0} 已和服务器 {1} 断开连接.", DateTime.Now.ToString("HH:mm:ss"), this.Host));
            }
        }
        #endregion

        #region 邮件操作
        /// <summary>
        /// 获取邮箱的状态
        /// </summary>
        /// <param name="numberOfMails">邮件总数</param>
        /// <param name="mailboxSize">邮箱的大小</param>
        /// <returns></returns>
        public void GetMailboxStats(out int numberOfMails, out int mailboxSize)
        {
            this.EnsureState(POP3ClientState.Connected);

            numberOfMails = 0;
            mailboxSize = 0;
            string data;
            if (this.SendCommand("STAT", out data))
            {
                //返回的数据格式样例: +OK 1 1000
                string[] d = data.Split(' ');
                if (d.Length > 2)
                {
                    numberOfMails = Utility.ToInt32(d[1]);
                    mailboxSize = Utility.ToInt32(d[2]);
                }                
            }
        }

        /// <summary>
        /// 获取邮件的信息列表
        /// </summary>
        /// <param name="containUID">是否包含UID值</param>
        /// <returns></returns>
        public List<MailInfo> GetMailList(bool containUID)
        {
            this.EnsureState(POP3ClientState.Connected);

            List<MailInfo> mailIds = new List<MailInfo>();
            string data;
            if (this.SendCommand("LIST", out data))
            {
                while (this.ReadMultiLineData(out data))
                {
                    string[] d = data.Split(' ');
                    if (d.Length > 1)
                    {
                        MailInfo e = new MailInfo(Utility.ToInt32(d[0]), string.Empty, Utility.ToInt32(d[1]));
                        mailIds.Add(e);
                    }
                }
            }
            if (containUID)
            {
                //获取UID值
                if (this.SendCommand("UIDL", out data))
                {
                    while (this.ReadMultiLineData(out data))
                    {
                        string[] d = data.Split(' ');
                        if (d.Length > 1)
                        {
                            MailInfo e = mailIds.Find(x => x.Id.ToString().Equals(d[0]));
                            if (e != null)
                            {
                                e.UID = d[1];
                            }
                        }
                    }
                }
            }
            return mailIds;
        }

        /// <summary>
        /// 获取某封邮件的大小
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public int GetMailSize(int mailId)
        {
            this.EnsureState(POP3ClientState.Connected);

            int size = -1;
            string data;
            if (this.SendCommand("LIST " + mailId.ToString(), out data))
            {
                //返回的数据格式样例: +OK 1 1000
                string[] d = data.Split(' ');
                if (d.Length > 2)
                {
                    size = Utility.ToInt32(d[2]);
                }
            }
            return size;
        }

        /// <summary>
        /// 获取某封邮件的UID值
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public string GetMailUID(int mailId)
        {
            this.EnsureState(POP3ClientState.Connected);

            string uid = string.Empty;
            string data;
            if (this.SendCommand("UIDL " + mailId.ToString(), out data))
            {
                //返回的数据格式样例: +OK 1 1tbirRtoBEX9bmLefAACsO
                string[] d = data.Split(' ');
                if (d.Length > 2)
                {
                    uid = d[2];
                }
            }
            return uid;
        }

        /// <summary>
        /// 获取某封邮件的原始内容
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public string GetMailContent(int mailId)
        {
            this.EnsureState(POP3ClientState.Connected);

            string data = null;
            if (this.SendCommand("RETR " + mailId, out data))
            {
                data = connectionStreamReader.ReadMultiLineAsString(this.Charset);
                TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), data));
            }
            return data;
        }
        /// <summary>
        /// 获取某封邮件的原始数据
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public byte[] GetMailData(int mailId)
        {
            this.EnsureState(POP3ClientState.Connected);

            string data;
            if (this.SendCommand("RETR " + mailId, out data))
            {
                byte[] buffer = connectionStreamReader.ReadMultiLine();
                if (buffer.Length > 0)
                {
                    TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), this.Charset.GetString(buffer)));
                    return buffer;
                }
            }
            return null;
        }
        /// <summary>
        /// 下载某封邮件并且保存到某文件
        /// </summary>
        /// <param name="mailId"></param>
        /// <param name="fileName"></param>
        public bool DownloadMail(int mailId, string fileName)
        {
            this.EnsureState(POP3ClientState.Connected);

            string data;
            if (this.SendCommand("RETR " + mailId, out data))
            {
                byte[] buffer = connectionStreamReader.ReadMultiLine();
                if (buffer.Length > 0)
                {
                    TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), this.Charset.GetString(buffer)));
                    using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取某封邮件消息
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public MailMessage GetMailMessage(int mailId)
        {
            byte[] data = GetMailData(mailId);
            if (data != null)
            {
                using (MemoryStream stream = new MemoryStream(data, false))
                {
                    return MimeParser.Parse(stream, this.Charset);
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取某封邮件的摘要
        /// </summary>
        /// <param name="mailId">邮件Id</param>
        /// <param name="rows">要获取的摘要行数</param>
        /// <returns></returns>
        public string GetMailSummary(int mailId, int rows)
        {
            this.EnsureState(POP3ClientState.Connected);

            string data = null;
            if (this.SendCommand(string.Format("TOP {0} {1}", mailId, rows), out data))
            {
                data = connectionStreamReader.ReadMultiLineAsString(this.Charset);
                TraceData(string.Format("{0} 收到: {1}", DateTime.Now.ToString("HH:mm:ss"), data));
            }
            return data;
        }
        /// <summary>
        /// 删除某封邮件
        /// </summary>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public bool DeleteMail(int mailId)
        {
            this.EnsureState(POP3ClientState.Connected);

            string data;
            return this.SendCommand("DELE " + mailId.ToString(), out data);
        }

        /// <summary>
        /// 恢复所有标记为已删除的邮件
        /// </summary>
        /// <returns></returns>
        public bool ResumeAllDeletedMails()
        {
            this.EnsureState(POP3ClientState.Connected);

            string data;
            return this.SendCommand("RSET", out data);
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
