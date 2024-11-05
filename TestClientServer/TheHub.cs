using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TestClientServer
{
    internal class TheHub : Microsoft.AspNetCore.SignalR.Hub
    {
        bool IsDisposed = true;
        private Task CallTask;
        public TheHub()
        {
            CallTask = Task.Factory.StartNew(
                () =>
                {
                    PostMessage(null, null);
                    Thread.Sleep(1000);
                }
            );           
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        public override Task OnConnectedAsync()
        {
            IsDisposed = false;
            return base.OnConnectedAsync();
        }

        public void PostMessage(object sender, ElapsedEventArgs e)
        {
            if (!IsDisposed)
                Clients?.All.SendAsync("TestOp");                                                                        
        }

        public bool Active() => true;
    }
}
