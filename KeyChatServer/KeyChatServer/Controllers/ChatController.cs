using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KeyChatServer.Models;
using System.Threading;

namespace KeyChatServer.Controllers
{
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
        static object _usersLocker = new object();
        static List<UserInfo> _users = new List<UserInfo>();

        static object _messagesLocker = new object();
        static Stack<Message> _messages = new Stack<Message>();

        static HashSet<AutoResetEvent> _resets = new HashSet<AutoResetEvent>();
        
        static ChatController()
        {

        }

        [HttpGet("ping")]
        public string ping()
        {
            return "pong";
        }

        [HttpGet("authorize")]
        public UserInfo Authorize()
        {
            var name = Request.Query["name"][0] as string;
            name = System.Net.WebUtility.UrlDecode(name);
            lock (_usersLocker)
            {
                var user = new UserInfo() { key = Guid.NewGuid(), nick = name };
                _users.Add(user);
                return user;
            }
        }

        [HttpGet("history")]
        public object history()
        {
            return _messages.ToArray();
        }

        [HttpGet("longpool")]
        public async Task<object> messages()
        {
            var ws = Convert.ToInt16(Request.Query["ws"][0]);
            var outMsgs = new List<Message>();
            var start = DateTime.UtcNow;

            var reset = new AutoResetEvent(false);
            _resets.Add(reset);
            reset.WaitOne(ws * 1000);
            _resets.Remove(reset);

            foreach (var msg in _messages.TakeWhile(m => m.time_stamp > start))
            {
                outMsgs.Add(msg);
            }
            return outMsgs.ToArray();
        }

        [HttpGet("send")]
        public object send()
        {
            var text = Request.Query["message"][0] as string;
            text = System.Net.WebUtility.UrlDecode(text);
            var key = Request.Query["key"][0] as string;
            
            try {
                var guid = Guid.Parse(key);
                var message = new Message
                {
                    text = text,
                    key = guid,
                    time_stamp = DateTime.UtcNow,
                    author_nick = _users.First(u => u.key == guid).nick
                };
                lock (_messagesLocker)
                {
                    _messages.Push(message);
                    foreach (var reset in _resets)
                    {
                        reset.Set();
                    }
                }
                return new { result = "OK" };
            }
            catch(Exception ex)
            {
                return new { result = "Error" };
            }
        }
    }
}
