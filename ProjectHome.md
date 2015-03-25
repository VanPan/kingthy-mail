# Kingthy.Mail.POP3 #
> 根据POP3协议进行邮件接收.
```
   例子:
            POP3Client client = new POP3Client("pop.gmail.com", 995, "xx@gmail.com", "xx", true);
            client.IsDebugMode = true;
            client.Connect();
            int num, size;
            client.GetMailboxStats(out num, out size);
            if (num > 0)
            {
                var list = client.GetMailList(false);
                foreach (var item in list)
                {
                    if (item.Id > 5) break;
                    client.Timeout = 600000;
                    var mail = client.GetMailMessage(item.Id);
                    Console.WriteLine(mail.Body);
                }
            }

```

# Kingthy.Mail.IMAP4 #
> 根据IMAP4协议对邮箱进行管理和接收邮件
```
   例子:
            IMAP4Client client = new IMAP4Client("imap.gmail.com", 993, "xxx@gmail.com", "xxx", true);
            client.IsDebugMode = true;
            client.Connect();
            var mailboxes = client.GetAllMailboxes();
            var mailbox = mailboxes.Find(x => x.Name.Equals("INBOX", StringComparison.InvariantCultureIgnoreCase));
            var status = mailbox.Select();

            for (int i = 1; i <= status.TotalMessages; i++)
            {
                var mail = mailbox.GetMailMessage(i);
                Console.WriteLine(mail.Body);
            }
```

# Kingthy.Mail.Mime #
> 用于解析MIMI格式的邮件内容,如.eml文件
```
   例子:
   MailMessage mail = MimeParser.Parse(@"c:\mailboxes\1.eml");
   Console.WriteLine(mail.Body);
```
