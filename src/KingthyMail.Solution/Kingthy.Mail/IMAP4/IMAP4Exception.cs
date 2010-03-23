/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  IMAP4Exception
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace Kingthy.Mail.IMAP4
{
    /// <summary>
    /// 
    /// </summary>
    public class IMAP4Exception : ApplicationException
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public IMAP4Exception(string message) : base(message) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public IMAP4Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
