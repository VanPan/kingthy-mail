using System;
using System.Collections.Generic;
using System.Text;
using Kingthy.Mail;
using System.IO;
using Kingthy.Mail.POP3;
using Kingthy.Mail.IMAP4;

namespace Kingthy.Mail.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            //POP3Client client = new POP3Client("pop.gmail.com", 995, "xx@gmail.com", "xx", true);
            //client.IsDebugMode = true;
            //client.Connect();
            //int num, size;
            //client.GetMailboxStats(out num, out size);
            //if (num > 0)
            //{
            //    var list = client.GetMailList(false);
            //    foreach (var item in list)
            //    {
            //        if (item.Id > 5) break;
            //        client.Timeout = 600000;
            //        var mail = client.GetMailMessage(item.Id);
            //        Console.WriteLine(mail.Body);
            //    }
            //}

            IMAP4Client client = new IMAP4Client("imap.gmail.com", 993, "xxx@gmail.com", "xxx", true);
            client.IsDebugMode = true;
            client.Connect();
            var mailboxes = client.GetAllMailboxes();
            var mailbox = mailboxes.Find(x => x.Name.Equals("INBOX", StringComparison.InvariantCultureIgnoreCase));
            var status = mailbox.Select();

            for (int i = 1; i <= status.TotalMessages; i++)
            {
                mailbox.DownloadMail(i, @"mail_" + i.ToString() + ".eml");
                Console.WriteLine("{0} Size = {1}", i, mailbox.GetMailSize(i));
                if (i > 9) break;
            }

            //Console.WriteLine(mailbox.Name);

            //client.Disconnect();
            //MailMessage mail = MimeParser.Parse(@"F:\Temporary\Mailboxes\1.eml");
            //Console.WriteLine(mail.Body);
            Console.ReadLine();
        }
    }
}
