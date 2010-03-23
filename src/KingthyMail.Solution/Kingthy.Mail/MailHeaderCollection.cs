/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  MailHeaderCollection
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace Kingthy.Mail
{
    /// <summary>
    /// 
    /// </summary>
    public class MailHeaderCollection : NameValueCollection
    {
        /// <summary>
        /// 
        /// </summary>
        internal MailHeaderCollection()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addHandler"></param>
        internal MailHeaderCollection(AddHeaderHandler addHandler)
        {
            this.AddHandler = addHandler;
        }

        /// <summary>
        /// 添加项时的事件委托
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        internal delegate void AddHeaderHandler(string name, string value);
        /// <summary>
        /// 
        /// </summary>
        private AddHeaderHandler AddHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void Add(string name, string value)
        {
            base.Add(name, value);
            if (AddHandler != null) AddHandler(name, value);
        }
    }
}
