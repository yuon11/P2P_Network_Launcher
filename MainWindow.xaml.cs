/////////////////////////////////////////////////////////////////////
// P2P_Network_Launcher - Final PRoject                             //
// ver 1, IP2P_Comm                                                 //
// Yuon Flemming, CSE681                                            //
/////////////////////////////////////////////////////////////////////
///
/*
 * The MainWindow class feeds data between the COMMPROTOCOL and SENDER
 * 
 * At PRogram start we test various Ports, and establish a connection. If no open port the user can select
 * 
 * Threads handling outgoing and incoming requests and information:
 * Results Thread
 * Search Thread
 * Recieved Message Thread
 * Sender Thread
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.ServiceModel;
using System.Globalization;

namespace P2P_Network_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CommProtocol recvr;
        Sender sendr;

        // string rcvdMsg = "";
        FileInfo rcvdMsg;
        List<FileInfo> outMsg;
        List<FileInfo> rsltMsg;

        int MaxMsgCount = 100;
        int listener_port = 8080;
        string listener_endpoint;

        Thread rcvThrd = null;
        Thread sendrThrd = null;
        Thread rsltThrd = null;
        Thread srchThrd = null;

        delegate void NewMessage(FileInfo msg);
        delegate void NewResultMessage(List<FileInfo> msg);
        event NewMessage OnNewMessage;
        event NewMessage OnNewSearchRequest;
        event NewResultMessage OnNewQuery;
        event NewResultMessage OnNewResult;

        public MainWindow()
        {
            InitializeComponent();
            Title = "P2P Network Launcher";
            OnNewMessage += new NewMessage(OnNewMessageHandler);
            OnNewSearchRequest += new NewMessage(OnNewSearchRequestHandler);
            OnNewQuery += new NewResultMessage(OnNewQueryHandler);
            OnNewResult += new NewResultMessage(OnNewResultHandler);

            listener_port = int.Parse(LocalPortTextBox.Text.ToString());
            listener_endpoint = "http://localhost:" + listener_port + "/IP2P_Comm";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            startListening();
            // startReceivingThreads();

        }

        void startReceivingThreads()
        {
            rcvThrd = new Thread(new ThreadStart(MessageThreadProc));
            sendrThrd = new Thread(new ThreadStart(OutgoingThreadProc));
            rsltThrd = new Thread(new ThreadStart(ResultsThreadProc));
            srchThrd = new Thread(new ThreadStart(SearchRequestThreadProc));
            

            rcvThrd.IsBackground = true;
            rcvThrd.Start();

            sendrThrd.IsBackground = true;
            sendrThrd.Start();

            rsltThrd.IsBackground = true;
            rsltThrd.Start();

            srchThrd.IsBackground = true;
            srchThrd.Start();
        }

        void startListening()
        {
            try
            {
                recvr = new CommProtocol();
                sendr = new Sender();

                listener_port = int.Parse(LocalPortTextBox.Text.ToString());
                listener_endpoint = "http://localhost:" + listener_port + "/IP2P_Comm";
                recvr.CreateRecvChannel(listener_endpoint);

                Console.WriteLine("Start Listening - " + listener_endpoint);
                
                
                
                startReceivingThreads();
                activateButtons();
            }
            catch (Exception ex)
            {
                listener_port++;
                Dispatcher.Invoke(() =>
                {
                    LocalPortTextBox.Text = listener_port.ToString();
                    deactivateButtons();
                });
                if ((listener_port - 4040) > 10000)
                {
                    startListening();
                    deactivateButtons();
                }
                Console.WriteLine(ex.Message);
            }
        }

        void activateButtons()
        {
            Dispatcher.Invoke(() =>
            {
                RunButton.IsEnabled = true;
                ListenButton.IsEnabled = false;
                Add_To_Share_Button.IsEnabled = true;
                Refresh_Folders_Button.IsEnabled = true;
                Remove_Folder_Button.IsEnabled = true;

                IncomingTransacts_Listbox.Items.Add("Network Listener Active at " + listener_endpoint);
            });
        }

        void deactivateButtons()
        {
            Dispatcher.Invoke(() =>
            {
                ListenButton.IsEnabled = true;
                RunButton.IsEnabled = false;
                Add_To_Share_Button.IsEnabled = false;
                Refresh_Folders_Button.IsEnabled = false;
                Remove_Folder_Button.IsEnabled = false;

                IncomingTransacts_Listbox.Items.Add("Network Listener at " + recvr.ToString() + " Deactivated.");
            });
        }


        //----< Run User Query Agains Checked Servers >-----------------------------------------
        private void Run_Query_Click(object sender, RoutedEventArgs e)
        {
            List<string> query_targets = Get_Query_Targets();
            if (File_CheckBox.IsChecked == true)
            {
                Run_FileSearch(query_targets);
            }
            if (DownloadFile_CheckBox.IsChecked == true)
            {
                Run_FileDownload(query_targets);
            }
            if (LogSearch_CheckBox.IsChecked == true)
            {
                Run_LogSearch(query_targets);
            }
        }

        private void Run_FileDownload(List<string> query_targets)
        {
            
            foreach (string remote_endpoint in query_targets)
            {
                FileInfo fi = new FileInfo(Download_Textbox.Text, listener_endpoint);
                fi.Destination = remote_endpoint;
                sendr.DownloadRequest(fi);
                Dispatcher.Invoke(() =>
                {
                    FormatResults(fi, "Download Request");
                });
            }
        }

        private void Run_FileSearch(List<string> query_targets)
        {
            //
            // List or Dictionary containing task info can be logged
            foreach (string target_host in query_targets)
            {
                FileInfo fi = new FileInfo(File_TextBox.Text, listener_endpoint, Description_TextBox.Text, Keywords_TextBox.Text);
                fi.Destination = target_host;
                sendr.SearchRequest(fi);
                Dispatcher.Invoke(() =>
                {
                    FormatResults(fi, "Search Query");
                });
            }
        }

        private void Run_LogSearch(List<string> query_targets)
        {
            foreach (string target_host in query_targets)
            {
                FileInfo fi = new FileInfo(LogSearch_Textbox.Text, listener_endpoint);
                fi.Destination = target_host;
                sendr.SearchRequest(fi);
                Dispatcher.Invoke(() =>
                {
                    FormatResults(fi, "Log Search");
                });
            }
        }

        //----< get target servers >-----------------------------------------
        private List<string> Get_Query_Targets()
        {
            List<string> query_targets = new List<string>();
            foreach (CheckBox server_checkbox in Server_Stack_Panel.Children)
            {
                if (server_checkbox.IsChecked == true)
                {
                    query_targets.Add(server_checkbox.Content.ToString());
                }
            }
            return query_targets;
        }

        //----< start listener >-----------------------------------------
        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                startListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                msg.Append("\nport = ");
                msg.Append(listener_port.ToString());
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }


        //----< stop listener >-----------------------------------------
        private void Stop_ListenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread closeComms = new Thread(() => {
                    recvr.Close();
                    //rcvThrd.Abort();
                    //sendrThrd.Abort();
                    //rsltThrd.Abort();
                    //srchThrd.Abort()
                });
                closeComms.IsBackground=true;
                closeComms.Start();

                deactivateButtons();
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }
        //----< receive thread processing >------------------------------
        void MessageThreadProc()
        {
            while (true)
            {
                rcvdMsg = recvr.GetMessage();
                Console.WriteLine("Get message in Message Thread : " + rcvdMsg.Endpoint + " Destination - " + rcvdMsg.Destination);
                Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewMessage,
                  rcvdMsg);
            }
        }

        void SearchRequestThreadProc()
        {
            while (true)
            {
                rcvdMsg = recvr.GetSearchRequest();
                Console.WriteLine("Get search request in search request Thread - Endpoint: " + rcvdMsg.Endpoint + " Destination - " + rcvdMsg.Destination);
                Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewSearchRequest,
                  rcvdMsg);
            }
        }

        void OutgoingThreadProc()
        {
            while (true)
            {
                outMsg = sendr.getResult();
                Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewQuery,
                  outMsg);
            }
        }

        void ResultsThreadProc()
        {
            while (true)
            {
                rsltMsg = recvr.getResult();
                Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewResult,
                  rsltMsg);
            }
        }

        //----< called by UI thread when dispatched from rcvThrd >-------
        void OnNewMessageHandler(FileInfo msg)
        {
            FormatInMessages(msg, "Request From");
        }

        void OnNewSearchRequestHandler(FileInfo msg)
        {
            FormatInMessages(msg, "Request From");
        }

        void OnNewResultHandler(List<FileInfo> res)
        {
            foreach (FileInfo result in res)
            {
                Console.WriteLine("Looping Over Results to display " + result.Name);
                FormatResults(result, "Results From");
            }
        }

        void OnNewQueryHandler(List<FileInfo> res)
        {
            foreach (FileInfo result in res)
            {
                Console.WriteLine("Looping Over Queries to display " + result.Name);
                FormatResults(result, "Query To");
            }
        }
        
        void FormatResults(FileInfo fi, string details = "Result/Query")
        {
            Console.WriteLine("In Format Results " + fi.Name);
            StackPanel resultStack = new StackPanel();

            TextBox queryTarget = new TextBox { Text = fi.Endpoint, Name = "Target", IsReadOnly = true };
            TextBox queryFile = new TextBox { Text = fi.Name, Name = "FileName", IsReadOnly = true };
            TextBox queryDescription = new TextBox { Text = fi.Description, Name = "Description", IsReadOnly = true };
            TextBox queryKeyword = new TextBox { Text = fi.Keywords, Name = "Keywords", IsReadOnly = true };
            TextBox queryTime = new TextBox { Text = fi.Timestamp, Name = "Timestamp", IsReadOnly = true };

            resultStack.Children.Add(queryTarget);
            resultStack.Children.Add(queryTime);
            resultStack.Children.Add(queryFile);
            if(fi.Destination.Length >0)
            {
                TextBox queryDestination = new TextBox { Text = fi.Destination, Name = "Timestamp", IsReadOnly = true };
                resultStack.Children.Add(queryDestination);
            }
            resultStack.Children.Add(queryDescription);
            resultStack.Children.Add(queryKeyword);

            Expander resultExpander = new Expander { Header = details +": " + fi.Endpoint, Content = resultStack };
            
            Console.WriteLine("Adding result expander to listbox");
            Grid newGrid = new Grid();
            newGrid.Children.Add(resultExpander);

            QueryResult_Listbox.Items.Add(newGrid);
            if (QueryResult_Listbox.Items.Count > MaxMsgCount)
                QueryResult_Listbox.Items.RemoveAt(QueryResult_Listbox.Items.Count - 1);
        }

        void FormatInMessages(FileInfo fi, string details = "Request")
        {
            Console.WriteLine("In Format Message " + fi.Name);
            StackPanel resultStack = new StackPanel();

            TextBox queryTarget = new TextBox { Text = fi.Endpoint, Name = "Target", IsReadOnly = true };
            TextBox queryFile = new TextBox { Text = fi.Name, Name = "FileName", IsReadOnly = true };
            TextBox queryDescription = new TextBox { Text = fi.Description, Name = "Description", IsReadOnly = true };
            TextBox queryKeyword = new TextBox { Text = fi.Keywords, Name = "Keywords", IsReadOnly = true };
            TextBox queryTime = new TextBox { Text = fi.Timestamp, Name = "Timestamp", IsReadOnly = true };

            resultStack.Children.Add(queryTarget);
            resultStack.Children.Add(queryTime);
            resultStack.Children.Add(queryFile);
            if (fi.Destination != null)
            {
                TextBox queryDestination = new TextBox { Text = fi.Destination, Name = "Timestamp", IsReadOnly = true };
                resultStack.Children.Add(queryDestination);
            }
            resultStack.Children.Add(queryDescription);
            resultStack.Children.Add(queryKeyword);

            Expander resultExpander = new Expander { Header = details + ": " + fi.Endpoint, Content = resultStack };

            Grid newGrid = new Grid();
            newGrid.Children.Add(resultExpander);
            IncomingTransacts_Listbox.Items.Add(newGrid);
            if (IncomingTransacts_Listbox.Items.Count > MaxMsgCount)
                IncomingTransacts_Listbox.Items.RemoveAt(IncomingTransacts_Listbox.Items.Count - 1);
            
        }
        //----< clear query inputs >-----------------------------------------
        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                File_CheckBox.IsChecked = false;
                DownloadFile_CheckBox.IsChecked = false;
                LogSearch_CheckBox.IsChecked = false;

                File_TextBox.Text = "";
                Description_TextBox.Text = "";
                Keywords_TextBox.Text = "";
            });
        }
        
        //----< check all server boxes >-----------------------------------------
        private void Select_All_Servers_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox server_checkbox in Server_Stack_Panel.Children)
            {
                Dispatcher.Invoke(() =>
                {
                    server_checkbox.IsChecked = true;
                });
                
            }
        }
        
        //----< uncheck all server boxes >-----------------------------------------
        private void Unselect_All_Servers_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox server_checkbox in Server_Stack_Panel.Children)
            {
                Dispatcher.Invoke(() =>
                {
                    server_checkbox.IsChecked = false;
                });
            }
        }
        
        //----< add a new server to the list >-----------------------------------------
        private void Add_Server_Click(object sender, RoutedEventArgs e)
        {
            AddServer serverWindow = new AddServer();            
            if (serverWindow.ShowDialog() == true)
            {
                Dispatcher.Invoke(() =>
                {
                    CheckBox serverBox = new System.Windows.Controls.CheckBox { Content = serverWindow.RemoteAddressText, Margin = new Thickness(10, 0, 0, 0) };
                    Server_Stack_Panel.Children.Add(serverBox);
                });
            }
        }

        //----< add folders to display >-----------------------------------------
        private void Add_Folders_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();

            if (result.ToString() != string.Empty)
            {
                string textPath = openFileDlg.SelectedPath;
                string dirName = textPath.Split('\\').Last().Trim();
                var newExpander = new Expander { Header=textPath };
                
                // Get a list of files in that directory
                var newstackPanel = new StackPanel();
                Shared_Directory_list.Items.Add(newExpander);
                newExpander.Content = newstackPanel;
                var fileStackPanel = (StackPanel)newExpander.Content;
                string[] fileEntries = Directory.GetFiles(textPath);

                foreach (string fileName in fileEntries)
                {
                    FileInfo fi = new FileInfo(fileName, listener_endpoint);
                    sendr.NewFile(fi);
                    Dispatcher.Invoke(() =>
                    {
                        update_file_expanders(fileStackPanel, fi);
                        FormatInMessages(fi, "Indexing");
                    });
                    
                }
            }
        }

        private void update_file_expanders(StackPanel parentStack, FileInfo fileName)
        {
            var descNKeywrdStackPanel = new StackPanel();
            var descTextBox = new TextBox { Text = fileName.Description, Name = "Description", Margin = new Thickness(30, 0, 0, 0) };
            var keywordTextBox = new TextBox { Text = fileName.Keywords, Name = "Keywords", Margin = new Thickness(30, 0, 0, 0) };
            var TimestampTextBox = new TextBox { Text = fileName.Timestamp, Name = "Timestamp", Margin = new Thickness(30, 0, 0, 0) };

            descNKeywrdStackPanel.Children.Add(descTextBox);
            descNKeywrdStackPanel.Children.Add(keywordTextBox);
            descNKeywrdStackPanel.Children.Add(TimestampTextBox);

            parentStack.Children.Add(new Expander { Header = fileName.Name.Split('\\').Last().Trim(), Margin = new Thickness(30, 0, 0, 0), Content = descNKeywrdStackPanel });
        }

        //----< update displayed folders >-----------------------------------------
        private void refresh_folders_click(object sender, RoutedEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                Add_New_Files();
                Remove_Old_Files();
            });
        }

        private void Remove_Folder_Click(object sender, RoutedEventArgs args)
        {
            if (Shared_Directory_list.SelectedItems.Count != 0)
            {
                while (Shared_Directory_list.SelectedIndex != -1)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shared_Directory_list.Items.RemoveAt(Shared_Directory_list.SelectedIndex);
                    });
                }
            }
        }

        //----< utility function to remove old folders >-----------------------------------------
        private void Remove_Old_Files()
        {
            foreach (object child in Shared_Directory_list.Items)
            {
                // This is a directory
                var oldExpander = (Expander)child;
                if (Directory.Exists(oldExpander.Header.ToString()))
                {
                    // Grab current files from directory
                    string[] fileEntries = Directory.GetFiles(oldExpander.Header.ToString());
                    var expanderFileStack = (StackPanel)oldExpander.Content;
                    List<Expander> toDelete = new List<Expander>();

                    foreach (object fileStack in expanderFileStack.Children)
                    {
                        var oldFileExpander = (Expander)fileStack;
                        string fileDir = oldFileExpander.Header.ToString();
                        bool isObsolete = true;

                        foreach (string fileName in fileEntries)
                        {
                            // Check if file is referenced
                            if (fileName.Split('\\').Last().Trim().Equals(oldFileExpander.Header.ToString()))
                            {
                                isObsolete = false;
                                break;
                            }
                        }

                        // The file is not referenced
                        if (isObsolete)
                        {
                            toDelete.Add(oldFileExpander);
                        }
                    }

                    foreach(Expander expanderToDelete in toDelete)
                    {
                        expanderFileStack.Children.Remove(expanderToDelete);
                    }
                }
            }
        }

    //----< utility function to add new folders >-----------------------------------------
        private void Add_New_Files()
        {
            foreach (Expander oldExpander in Shared_Directory_list.Items)
            {
                // This is a directory
                if (Directory.Exists(oldExpander.Header.ToString()))
                {
                    // Grab current files from directory
                    string[] fileEntries = Directory.GetFiles(oldExpander.Header.ToString());
                    var expanderFileStack = (StackPanel)oldExpander.Content;

                    foreach (string fileName in fileEntries)
                    {
                        bool islisted = false;
                        foreach (Expander fileStack in expanderFileStack.Children)
                        {
                            var oldFileExpander = (Expander)fileStack;
                            string fileDir = oldFileExpander.Header.ToString();

                            if (fileName.Split('\\').Last().Trim().Equals(oldFileExpander.Header.ToString()))
                            {
                                string desc = "";
                                string keywords = "";

                                StackPanel fileDetails = (StackPanel)fileStack.Content;
                                foreach (TextBox item in fileDetails.Children)
                                {
                                    if (item.Name == "Description")
                                    {
                                        desc = item.Text.ToString();
                                    }
                                    else if (item.Name == "Keywords")
                                    {
                                        keywords = item.Text;
                                    }
                                }
                                FileInfo fi = new FileInfo(fileName, listener_endpoint, desc, keywords);
                                sendr.NewFile(fi);
                                Dispatcher.Invoke(() =>
                                {
                                    expanderFileStack.Children.Remove(oldFileExpander);
                                    update_file_expanders(expanderFileStack, fi);
                                    FormatInMessages(fi, "Indexing");
                                });
                                islisted = true;
                                break;
                            }
                        }
                        // The file is not referenced so we can add it
                        if (!islisted)
                        {
                            FileInfo fi = new FileInfo(fileName, listener_endpoint);
                            sendr.NewFile(fi);
                            Dispatcher.Invoke(() =>
                            {
                                update_file_expanders(expanderFileStack, fi);
                                FormatInMessages(fi, "Indexing");
                            });

                        }
                        
                    }
                }
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            recvr.Close();
            recvr.closeFile();
            sendr.Close();

            sendrThrd.Abort();
            rcvThrd.Abort();
            rsltThrd.Abort();
        }
    }
}