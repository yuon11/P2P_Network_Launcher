using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace P2P_Network_Launcher
{
    /// <summary>
    /// Interaction logic for AddServer.xaml
    /// </summary>
    public partial class AddServer : Window
    {
        //----< connect to remote listener >-----------------------------
        public AddServer()
        {
            InitializeComponent();
            Title = "P2P Network Launcher - Add Server";

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string remoteAddress = RemoteAddressTextBox.Text;
            string endpoint = remoteAddress;
            int MaxMsgCount = 100;

            ConnectButton.IsEnabled = true;

            string who = endpoint;
            AutoResetEvent waiter = new AutoResetEvent(false);

            Ping pingSender = new Ping();

            // When the PingCompleted event is raised,  
            // the PingCompletedCallback method is called.  
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

            // Create a buffer of 32 bytes of data to be transmitted.  
            string data = "This is a test message";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply.  
            int timeout = 12000;

            // Set options for transmission:  
            // The data can go through 64 gateways or routers  
            // before it is destroyed, and the data packet  
            // cannot be fragmented.  
            PingOptions options = new PingOptions(64, true);

            Console.WriteLine("Time to live: {0}", options.Ttl);
            Console.WriteLine("Don't fragment: {0}", options.DontFragment);
            try
            {
                add_server_listbox.Items.Add("Time to live: "+ options.Ttl);
                add_server_listbox.Items.Add("Don't fragment: "+ options.DontFragment);

                if (add_server_listbox.Items.Count > MaxMsgCount)
                {
                    add_server_listbox.Items.RemoveAt(add_server_listbox.Items.Count - 1);
                }

                // Send the ping asynchronously.  
                // Use the waiter as the user token.  
                // When the callback completes, it can wake up this thread.  
                pingSender.SendAsync(who, timeout, buffer, options, waiter);
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }

            // Prevent this example application from ending.  
            // A real application should do something useful  
            // when possible.  
            // waiter.WaitOne();
            Console.WriteLine("Ping example completed.");
            add_server_listbox.Items.Add("Ping example completed.");
        }

        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.  
            if (e.Cancelled)
            {
                Console.WriteLine("Ping cancelled.");
                // add_server_listbox.Items.Add("Ping example completed.");

                // Let the main thread resume.
                // UserToken is the AutoResetEvent object that the main thread
                // is waiting for.  
                ((AutoResetEvent)e.UserState).Set();
            }

            // If an error occurred, display the exception to the user.  
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());
                

                // Let the main thread resume.
                ((AutoResetEvent)e.UserState).Set();
            }

            // PingReply reply = e.Reply;

            // DisplayReply(reply);

            // Let the main thread resume.  
            ((AutoResetEvent)e.UserState).Set();
        }

        public void DisplayReply(PingReply reply)
        {
            if (reply == null)
                return;

            Console.WriteLine("ping status: {0}", reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Address: {0}", reply.Address.ToString());
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);

                add_server_listbox.Items.Add("Address: "+ reply.Address.ToString());
                add_server_listbox.Items.Add("RoundTrip time: "+ reply.RoundtripTime);
                add_server_listbox.Items.Add("Time to live: "+ reply.Options.Ttl);
                add_server_listbox.Items.Add("Don't fragment: "+ reply.Options.DontFragment);
                add_server_listbox.Items.Add("Buffer size: "+ reply.Buffer.Length);
            }
        }

        public string RemoteAddressText
        {
            get { return RemoteAddressTextBox.Text; }
            set { RemoteAddressTextBox.Text = value; }
        }

        private void Add_Server_Click(object sender, RoutedEventArgs e)
        {
            // string remoteAddress = RemoteAddressTextBox.Text;
            DialogResult = true;
        }

        private void Cancel_Add_Folder_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
