using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeyChatServer.Models
{
    public class Message
    {
        public string text;
        public string author_nick;
        public Guid key;
        public DateTime time_stamp;
    }
}
