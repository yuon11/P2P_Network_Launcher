/////////////////////////////////////////////////////////////////////
// P2P_Network_Launcher - Final PRoject                             //
// ver 1, IP2P_Comm                                                 //
// Yuon Flemming, CSE681                                            //
/////////////////////////////////////////////////////////////////////
///
/*
 * This Program contains the class that defins the SENDER and COMPROTOCOL
 * 
 * The COMPROTOCOL will listen in the background for Any incoming Peer Request
 * Incoming requests are managed in a blockingqueue  servicing one of each
 * 
 * - Indexing - Update our receivers data dict with new files
 * 
 * - Search - Search data dictionary for matches to specified file data
 * 
 * - Download - send specified file to Requesting host 
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SWTools;
using System.Collections.Concurrent;
using System.IO;

namespace P2P_Network_Launcher
{
    /////////////////////////////////////////////////////////////
    // CommProtocol hosts Communication service used by other Peers
    public class CommProtocol : IP2P_Comm
    {
        FileStream fs = null;

        static BlockingQueue<FileInfo> downloadReqBlockingQ = null;
        static BlockingQueue<List<FileInfo>> searchResBlockingQ = null;
        static BlockingQueue<FileInfo> newFileBlockingQ = null;
        static BlockingQueue<FileInfo> searchFileBlockingQ = null;
        // static BlockingQueue<FileInfo> removeFileBlockingQ = null;
        static ConcurrentDictionary<string, ConcurrentBag<FileInfo>> indexedFiles;

        // public delegate void ListenerCreated(string localEndPoint);
        // public event ListenerCreated OnListenerCreated;
        int tryCount = 0, MaxCount = 10;

        ServiceHost service = null;
        void showAll()
        {
            foreach (string fileName in indexedFiles.Keys)
            {
                var temp = indexedFiles[fileName];
                foreach (FileInfo e in temp)
                {
                    Console.WriteLine(fileName + "--"  + temp + "--" + e.Endpoint + "--" + e.Description + "--" + e.Keywords);
                }
            }
        }

        private void SendResponse(FileInfo fileInfo, List<FileInfo> res)
        {
            try
            {
                string endpoint = fileInfo.Endpoint;
                EndpointAddress baseAddress = new EndpointAddress(endpoint);
                WSHttpBinding binding = new WSHttpBinding();
                ChannelFactory<IP2P_Comm> factory
                  = new ChannelFactory<IP2P_Comm>(binding, endpoint);
                IP2P_Comm channel = factory.CreateChannel();

                Console.WriteLine("SENDING RESPONSE: " + fileInfo.Name + " - From - " + fileInfo.Endpoint + " - to - " + fileInfo.Destination);
                channel.SearchResult(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " - " + fileInfo.Endpoint);
            }
        }

        // When Search request is receieved, we run this
        private void startSearchHandlerThread()
        {
            Thread searchThread = new Thread(() => {
                while (true)
                {
                    FileInfo searchReq = GetSearchRequest();
                    Console.WriteLine("File - "+searchReq.Name + "- From - " + searchReq.Endpoint + " - to" + searchReq.Destination);
                    List<FileInfo> res = new List<FileInfo> { };

                    // Check for file by name
                    // If not found by Name, check descriptions and/or keywords

                    try
                    {
                        List<FileInfo> descRes;
                        List<FileInfo> keywrdRes;

                        if (searchReq.Name.Length > 0)
                        {
                            res = indexNameSearch(searchReq);
                        }
                        if (searchReq.Description.Length > 0)
                        {
                            descRes = indexDescSearch(searchReq);
                            res = addToResults(res, descRes);
                        }
                        if (searchReq.Keywords.Length > 0)
                        {
                            keywrdRes = indexKeywordSearch(searchReq);
                            res = addToResults(res, keywrdRes);
                        }

                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        res.Add(new FileInfo("Failed To Find: " + searchReq.Name, searchReq.Endpoint, searchReq.Description, searchReq.Keywords));
                        SendResponse(searchReq, res);

                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            break;
                        }
                    }

                    Console.WriteLine("Search Request Thread - "+searchReq.Destination + "Search Request Thread - " + searchReq.Endpoint);
                    SendResponse(searchReq, res);
                }
            });
            searchThread.IsBackground = true;
            searchThread.Start();
        }

        List<FileInfo> addToResults(List<FileInfo> curr_res, List<FileInfo> incoming)
        {
            foreach (FileInfo new_file in incoming)
            {
                foreach (FileInfo file in curr_res) 
                {
                    if (!file.Name.Equals(new_file) && !file.Description.Equals(new_file))
                    {
                        curr_res.Add(new_file);
                    }
                }
            }
            return curr_res;
        }

        List<FileInfo> indexNameSearch(FileInfo searchReq)
        {
            FileInfo tmp;
            List<FileInfo> results = new List<FileInfo> { };
            foreach (string indexedFile in indexedFiles.Keys)
            {
                if (indexedFile.Contains(searchReq.Name))
                {
                    if (indexedFiles[indexedFile].TryPeek(out tmp))
                    {
                        results.Add(tmp);
                    }
                }
            }
            return results;
        }

        List<FileInfo> indexDescSearch(FileInfo searchReq)
        {
            FileInfo tmp;
            List<FileInfo> results = new List<FileInfo> { };

            foreach (string indexedFile in indexedFiles.Keys)
            {
                if (indexedFiles[indexedFile].TryPeek(out tmp))
                {
                    if (tmp.Description.Contains(searchReq.Description))
                    {
                        results.Add(tmp);
                    }
                }
            }
            return results;
        }

        List<FileInfo> indexKeywordSearch(FileInfo searchReq)
        {
            FileInfo tmp;
            List<FileInfo> results = new List<FileInfo> { };

            foreach (string indexedFile in indexedFiles.Keys)
            {
                if (indexedFiles[indexedFile].TryPeek(out tmp))
                {
                    foreach (string keyword in searchReq.Keywords.Split())
                    {
                        if (tmp.Keywords.Contains(keyword))
                        {
                            results.Add(tmp);
                            break;
                        }
                    }
                }
            }
            return results;
        }

        public CommProtocol()
        {
            if (newFileBlockingQ == null)
                newFileBlockingQ = new BlockingQueue<FileInfo>();

            if (searchFileBlockingQ == null)
                searchFileBlockingQ = new BlockingQueue<FileInfo>();

            if (searchResBlockingQ == null)
                searchResBlockingQ = new BlockingQueue<List<FileInfo>>();
            
            if (downloadReqBlockingQ == null)
                downloadReqBlockingQ = new BlockingQueue<FileInfo>();

            if (indexedFiles == null)
                indexedFiles = new ConcurrentDictionary<string, ConcurrentBag<FileInfo>>();
            IndexFiles();
            startSearchHandlerThread();
        }
        public void CreateRecvChannel(string address)
        {
            WSHttpBinding binding = new WSHttpBinding();
            Uri baseAddress = new Uri(address);
            service = new ServiceHost(typeof(CommProtocol), baseAddress);
            service.AddServiceEndpoint(typeof(IP2P_Comm), binding, baseAddress);
            service.Open();
        }
        // Implement service method to receive messages from other Peers

        // Implement service method to extract messages from other Peers.
        // This will often block on empty queue, so user should provide
        // read thread.
        public FileInfo GetSearchRequest()
        {
            return searchFileBlockingQ.deQ();
        }

        public void Search(FileInfo searchInfo)
        {
            searchFileBlockingQ.enQ(searchInfo);
        }

        public FileInfo GetMessage()
        {
            return newFileBlockingQ.deQ();
        }

        private void IndexFiles()
        {
            Thread rcvThrd = new Thread(() => {
                while (true)
                {
                    try
                    {
                        // get message out of receive queue - will block if queue is empty
                        FileInfo rcvdMsg = GetMessage();
                        Console.WriteLine("Get message in indexfiles - " + rcvdMsg.Endpoint);
                        if (indexedFiles.ContainsKey(rcvdMsg.Name))
                        {
                            indexedFiles[rcvdMsg.Name.Split('\\').Last().Trim()].Add(rcvdMsg);
                        }
                        else
                        {
                            ConcurrentBag<FileInfo> temp = new ConcurrentBag<FileInfo>();
                            temp.Add(rcvdMsg);
                            indexedFiles[rcvdMsg.Name.Split('\\').Last().Trim()] = temp;
                        }              // call window functions on UI thread
                        showAll();
                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            Console.WriteLine(ex.Message); 
                            break;
                        }
                    }
                }
            });
            rcvThrd.IsBackground = true;
            rcvThrd.Start();
        }

        public void NewFile(FileInfo fileInfo)
        {
            newFileBlockingQ.enQ(fileInfo);
        }

        void sendFile()
        {
            while (true)
            {
                FileInfo fileInfo = getDownLoadReq();
                string Name = fileInfo.Name;
                string target = fileInfo.Endpoint;
                Thread sendFileThread = new Thread(() => {
                    sendFile(Name, target);
                });
                sendFileThread.IsBackground = true;
                sendFileThread.Start();
            }
        }


        void sendFile(string name, string target)
        {
            string endpoint = target;
            EndpointAddress baseAddress = new EndpointAddress(endpoint);
            BasicHttpBinding binding = new BasicHttpBinding();
            ChannelFactory<IP2P_Comm> factory
              = new ChannelFactory<IP2P_Comm>(binding, endpoint);
            
            IP2P_Comm channel = factory.CreateChannel();
            

            FileStream fs_local = null;
            long bytesRemaining;

            try
            {
                fs_local = File.OpenRead("./" + name);
                bytesRemaining = fs_local.Length;
                channel.openFileForWrite(name);
                while (true)
                {
                    long bytesToRead = Math.Min(1024, bytesRemaining);
                    byte[] blk = new byte[bytesToRead];
                    long numBytesRead = fs_local.Read(blk, 0, (int)bytesToRead);
                    bytesRemaining -= numBytesRead;

                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }
                channel.closeFile();
                fs_local.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // channel.SearchResult(res);
        }

        public void Close()
        {
            service.Close();
        }

        //  Create ServiceHost for Communication service

        public void closeFile()
        {
            fs.Close();
        }

        public void DownloadRequest(FileInfo fileInfo)
        {
            downloadReqBlockingQ.enQ(fileInfo);
        }

        public bool openFileForWrite(string name)
        {
            try
            {
                string writePath = Path.Combine("./", name);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public void SearchResult(List<FileInfo> result)
        {
            searchResBlockingQ.enQ(result);
        }

        public List<FileInfo> getResult()
        {
            return searchResBlockingQ.deQ();
        }

        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public FileInfo getDownLoadReq()
        {
            return downloadReqBlockingQ.deQ();
        }
    }


    /*
     * This Program contains the class that defines the SENDER and COMPROTOCOL
     * 
     * The SENDER will organize any outgoing user Requests to Peers
     * The data in outgoing requests retain return information for the peer.
     * 
     * - Indexing - Update our receivers data dict with new files
     * 
     * - Search - Search data dictionary for matches to specified file data
     * 
     * - Download - send specified file to Requesting host 
     * 
     */

    public class Sender
    {
        IP2P_Comm channel;
        string lastError = "";
        static BlockingQueue<FileInfo> sndBlockingQ = null;
        static BlockingQueue<FileInfo> dwnldBlockingQ = null;
        static BlockingQueue<FileInfo> srchBlockingQ = null;

        static BlockingQueue<List<FileInfo>> searchResBlockingQ = null;

        Thread indxThrd = null;
        Thread dwnldThrd = null;
        Thread srchThrd = null;

        int tryCount = 0, MaxCount = 10;
        // ServiceHost service = null;


        void IndexThreadProc()
        {
            indxThrd = new Thread(() => {
                while (true)
                {
                    try
                    {
                        FileInfo msg = sndBlockingQ.deQ();
                        Console.WriteLine("Index Thread PRoc"+ msg.Endpoint);
                        CreateSendChannel(msg.Endpoint);
                        channel.NewFile(msg);
                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            lastError = ex.Message;
                            break;
                        }
                    }
                }
            });
            indxThrd.IsBackground = true;
            indxThrd.Start();
        }

        void DownloadThreadProc()
        {
            dwnldThrd = new Thread(() => {
                while (true)
                {
                    try
                    {
                        FileInfo msg = dwnldBlockingQ.deQ();
                        CreateSendChannel(msg.Destination);
                        channel.DownloadRequest(msg);
                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            lastError = ex.Message;
                            break;
                        }
                    }
                }
            });
            dwnldThrd.IsBackground = true;
            dwnldThrd.Start();
        }

        void SearchThreadProc()
        {
            srchThrd = new Thread(() => {
                while (true)
                {
                    try
                    {
                        FileInfo msg = srchBlockingQ.deQ();
                        
                        Console.WriteLine("Search Thread PRoc" + msg.Destination);
                        CreateSendChannel(msg.Destination);
                        channel.Search(msg);
                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            lastError = ex.Message;
                            break;
                        }
                    }
                }
            });
            srchThrd.IsBackground = true;
            srchThrd.Start();
        }

        // Create Communication channel proxy, sndBlockingQ, and
        // start sndThrd to send messages that client enqueues

        public Sender()
        {

            if (sndBlockingQ == null)
                sndBlockingQ = new BlockingQueue<FileInfo>();
            if (dwnldBlockingQ == null)
                dwnldBlockingQ = new BlockingQueue<FileInfo>();
            if (srchBlockingQ == null)
                srchBlockingQ = new BlockingQueue<FileInfo>();
            if (searchResBlockingQ == null)
                searchResBlockingQ = new BlockingQueue<List<FileInfo>>();

            SearchThreadProc();
            DownloadThreadProc();
            IndexThreadProc();

        }

        // Create proxy to another Peer's Communicator
        public void CreateSendChannel(string address)
        {
            Console.WriteLine("Sender Create Send Channel - " + address);
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            ChannelFactory<IP2P_Comm> factory
                = new ChannelFactory<IP2P_Comm>(binding, address);
            channel = factory.CreateChannel();
        }

        // Sender posts message to another Peer's queue using
        // Communication service hosted by receipient via sndThrd
        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }

        public void NewFile(FileInfo fileInfo)
        {
            sndBlockingQ.enQ(fileInfo);
        }

        public void DownloadRequest(FileInfo fileInfo)
        {
            dwnldBlockingQ.enQ(fileInfo);
        }

        public void SearchRequest(FileInfo fileInfo)
        {
            srchBlockingQ.enQ(fileInfo);
        }

        public void SearchResult(List<FileInfo> result)
        {

            searchResBlockingQ.enQ(result);
        }

        public List<FileInfo> getResult()
        {
            return searchResBlockingQ.deQ();
        }

        public void Close()
        {
            ChannelFactory<IP2P_Comm> temp = (ChannelFactory<IP2P_Comm>)channel;
            temp.Close();
        }
    }
}
