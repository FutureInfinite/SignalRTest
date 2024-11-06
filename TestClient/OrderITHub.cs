using Microsoft.AspNetCore.SignalR.Client;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestClient
{
    public class OrderITHubProxy : BindableBase
    {
        #region "Properties & Attributes"                
        protected Microsoft.AspNetCore.SignalR.Client.HubConnection ClientHubConnectionNew;
        protected static Microsoft.AspNetCore.SignalR.Client.HubConnection ClientHubConnectionNewSinlge;
        protected bool UseSingle { get; set; } = false;

        protected Microsoft.AspNetCore.SignalR.Client.HubConnection HubConnectionNew
        {
            get
            {
                if (UseSingle == null)
                    return ClientHubConnectionNew;
                else
                    return ClientHubConnectionNewSinlge;
            }
            set
            {
                //the use of a single hub connection requires
                //that the hub connecion is to the same HUB
                if (UseSingle == null)
                    ClientHubConnectionNew = value;
                else
                    ClientHubConnectionNewSinlge = value;
            }
        }

        //protected static Microsoft.AspNet.SignalR.Client.IHubProxy ClientHubProxy { get; set; } = null;
        protected static object PreparingHub = new object();
        protected bool Continue { get; set; } = true;
        protected bool ActivatingTradeInterface = false;
        protected static bool HubConnected = false;
        protected string Connection;

        public delegate void ClientHubActive();
        public event ClientHubActive ClientHubActiveated;

        private static System.Timers.Timer HubConnectionTimer;
        private static object WaitToActivateHub = new object();
        #endregion //"Properties & Attributes"

        #region "Lifetime"
        public OrderITHubProxy(
            string Connection,
            bool UseSingle = false
            )
        {
            System.Diagnostics.EventLog.WriteEntry("Support", string.Format("Start Hub connection for {0}", Connection), System.Diagnostics.EventLogEntryType.Information);

            HubConnectionTimer = new System.Timers.Timer();

            this.Connection = Connection;
            this.UseSingle = UseSingle;

            ///simple thread logic to start up 
            ///and maintain the connection
            ///to the trading interface
            //HubConnectionTimer.Elapsed += PrepareTradeActivityClient;            
            //HubConnectionTimer.Start();
            PrepareTradeActivityClient(null, null);
        }

        private void TestApp()
        {

        }

        ~OrderITHubProxy()
        {
            StopTradeClientInterface();
        }
        #endregion "Lifetime"

        #region "Operations"

        virtual protected void PrepareTradeActivityClient(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Factory.StartNew(
                   async () =>
                   {
                       while (Continue)
                       {
                           try
                           {
                               ActivateTradeClientInterface();
                           }
                           finally
                           {
                               ///5sec interval for trading interface refresh
                               Thread.Sleep(5000);
                           }
                       }
                   }
                   , TaskCreationOptions.LongRunning
               );

            //HubConnectionTimer.Interval = 5000;            
        }

        protected virtual bool PrepareTradeActivityHub()
        {
            string TradeActivitySignalRURL = "";
            bool Res = false;

            lock (PreparingHub)
            {
                TradeActivitySignalRURL = Connection;

                if (HubConnectionNew == null)
                {
                    System.Diagnostics.EventLog.WriteEntry("Support", string.Format("Prepare Trade Service Hub for {0}", TradeActivitySignalRURL), System.Diagnostics.EventLogEntryType.Information);


                    HubConnectionNew = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl(TradeActivitySignalRURL).Build();
                    HubConnectionNew.Closed += StockPusherHubDisconnect;
                    var Connect = StockPusherHubConnection_Connect();
                    Connect.Wait();

                    System.Diagnostics.EventLog.WriteEntry("Support", "Test calling Hub Active operation", System.Diagnostics.EventLogEntryType.Information);

                    var Response = HubConnectionNew.InvokeAsync<bool>("Active").Result;
                    if (!Response)
                    {
                        HubConnectionNew = null;
                        System.Diagnostics.EventLog.WriteEntry("Support", "Problem calling Hub Active operation", System.Diagnostics.EventLogEntryType.Error);
                        HubConnected = false;
                    }
                    else
                    {
                        System.Diagnostics.EventLog.WriteEntry("Support", "Successfully called Hub Active operation", System.Diagnostics.EventLogEntryType.Information);
                        Res = true;
                        HubConnected = true;
                        HubConnectionNew.ServerTimeout = TimeSpan.FromSeconds(100000);

                    }
                }
                else
                {
                    System.Diagnostics.EventLog.WriteEntry("Support", "Test calling Hub Active operation", System.Diagnostics.EventLogEntryType.Information);

                    var Response = HubConnectionNew.InvokeAsync<bool>("Active").Result;
                    if (!Response)
                    {
                        HubConnectionNew = null;
                        System.Diagnostics.EventLog.WriteEntry("Support", "Problem calling Hub Active operation", System.Diagnostics.EventLogEntryType.Error);
                        HubConnected = false;
                    }
                    else
                    {
                        System.Diagnostics.EventLog.WriteEntry("Support", "Successfully called Hub Active operation", System.Diagnostics.EventLogEntryType.Information);
                        Res = true;
                        HubConnected = true;
                    }
                }
            }
            return Res;
        }

        public virtual void TradingClientHubConnection_Closed()
        {
            StopHubConnection();
        }

        protected virtual void StopHubConnection()
        {
            try
            {
                if (HubConnectionNew != null && HubConnectionNew.State == HubConnectionState.Connected)
                {
                    //HubConnectionNew.StopAsync().Wait();
                    lock (PreparingHub)
                    {
                        HubConnectionNew = null;
                    }
                }
            }
            catch { }
            finally
            {
                HubConnected = false;
            }
        }

        private void StopTradeClientInterface()
        {
            try
            {
                StopHubConnection();
            }
            finally { }

        }

        /// <summary>
        /// activate trading services        
        /// </summary>
        protected virtual bool ActivateTradeClientInterface()
        {
            bool bRes = false;

            if (!ActivatingTradeInterface)
            {
                try
                {
                    ActivatingTradeInterface = true;

                    System.Diagnostics.EventLog.WriteEntry("oiTrader Client", "Attempt to connect to Trader Hub", System.Diagnostics.EventLogEntryType.Information);

                    if (PrepareTradeActivityHub())
                    {
                        bRes = true;
                        Action TestRequestResult = TestApp;
                        HubConnectionNew.On("TestOp", TestRequestResult);
                        //For test
                        HubConnectionNew.On("TestOp", () =>
                        {
                            MessageBox.Show("Receive a TestOp message from the server");
                        });

                    }
                    else
                        StopHubConnection();
                }
                catch (Exception ex)
                {
                    StopHubConnection();
                }
                finally { ActivatingTradeInterface = false; }
            }
            else
                bRes = HubConnected;

            return bRes;
        }

        protected virtual Task StockPusherHubDisconnect(Exception arg)
        {
            StockPusherHubConnection_Connect().Wait();

            return null;
        }

        protected virtual async Task StockPusherHubConnection_Connect()
        {
            Exception exception = null;
            Exception except = null;

            try
            {
                if (HubConnectionNew != null)
                {
                    lock (PreparingHub)
                    {
                        System.Diagnostics.EventLog.WriteEntry("Support", string.Format("Attempt to connect to Hub for {0}", Connection), System.Diagnostics.EventLogEntryType.Information);

                        var HubConnect = HubConnectionNew?.StartAsync();
                        HubConnect?.Wait();
                        HubConnect?.ContinueWith(
                            task =>
                            {
                                try
                                {
                                    if (task != null && task.IsFaulted)
                                    {
                                        System.Diagnostics.EventLog.WriteEntry("Support", "Problem connecting to Hub for", System.Diagnostics.EventLogEntryType.Error);
                                        System.Diagnostics.EventLog.WriteEntry("Support", task.Exception.Message, System.Diagnostics.EventLogEntryType.Error);

                                        exception = task?.Exception;
                                        except = exception?.InnerException;
                                        while (except != null)
                                        {
                                            except = except?.InnerException;
                                            if (except != null)
                                                System.Diagnostics.EventLog.WriteEntry("Support", except.Message, System.Diagnostics.EventLogEntryType.Error);
                                        }
                                    }
                                }
                                catch { }
                            }
                        ).Wait();
                    }
                }

            }
            catch (Exception ex)
            {
            }
        }

        protected virtual void StopStockPusher()
        {
            try
            {
                if (HubConnectionNew != null && HubConnectionNew.State == HubConnectionState.Connected)
                    HubConnectionNew.StopAsync();
            }
            catch { }

            HubConnectionNew = null;
        }

        protected void CallCheckActivated()
        {
            if (ClientHubActiveated != null) ClientHubActiveated();
        }
        #endregion //"Operations"
    }
}