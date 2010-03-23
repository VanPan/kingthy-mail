/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  POP3Exception
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace Kingthy.Mail.POP3
{
    /// <summary>
    /// 
    /// </summary>
    public class POP3Exception : ApplicationException
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public POP3Exception(string message) : base(message) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public POP3Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
