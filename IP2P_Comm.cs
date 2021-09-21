/////////////////////////////////////////////////////////////////////
// P2P_Network_Launcher - Final PRoject                             //
// ver 1, IP2P_Comm                                                 //
// Yuon Flemming, CSE681                                            //
/////////////////////////////////////////////////////////////////////
///
/*
 * The purpose of this Interface is to define the Service Contract and Date Contracts for the Sender and Receiver
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Network_Launcher
{
    [ServiceContract]
    public interface IP2P_Comm
    {
        // [OperationContract(IsOneWay = true)]
        // void PostMessage(string msg);

        // used only locally so not exposed as service method
        // [OperationContract(IsOneWay = true)]
        [OperationContract(IsOneWay = true)]
        void NewFile(FileInfo fileInfo);
        // used only locally so not exposed as service method

        [OperationContract]
        void Search(FileInfo fileInfo);

        [OperationContract]
        void SearchResult(List<FileInfo> result);
        
        // used only locally so not exposed as service method
        List<FileInfo> getResult();
        
        [OperationContract]
        void DownloadRequest(FileInfo fileInfo);

        /*---< called to open a file on Receiver >-----*/
        [OperationContract]
        bool openFileForWrite(string name);
        
        /*----< write a block received from Sender >----------*/
        [OperationContract]
        bool writeFileBlock(byte[] block);

        /*----< close file >-----------------------*/
        [OperationContract(IsOneWay = true)]
        void closeFile();
        FileInfo GetMessage();
        //string GetMessage();
    }

    [DataContract]
    public class FileInfo
    {
        [DataMember]
        public string Name;

        [DataMember]
        public string Endpoint;

        [DataMember]
        public string Description;

        [DataMember]
        public string Keywords;

        [DataMember]
        public string Destination;

        [DataMember]
        public string Timestamp;
        // DateTime utcDate = DateTime.UtcNow;

        public FileInfo(string newName, string newEndpoint, string description="", string keywords="")
        {
            Name = newName;
            Endpoint = newEndpoint;
            Description = description;
            Keywords = keywords;
            this.SetTimestamp();
        }

        void SetDestination(string target_host) 
        {
            Destination = target_host;
        }

        string GetDestination()
        {
            return Destination;
        }

        //

        void SetTimestamp()
        {
            Timestamp = DateTime.Now.ToString();
            // DateTime utcDate = DateTime.UtcNow; 
        }

        string GetTimestamp()
        {
            return Timestamp;
        }
    }
}
