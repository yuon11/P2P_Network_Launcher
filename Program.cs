using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace P2P_Network_WebApplication
{
    class Program
    {
        //
        // Main Function - Entry point for XML generation program
        //

        //
        //  Format Create XML Document Args
        //
        static string[] parseCreateXMLArgs(string[] args)
        {
            string file_name = "";
            string description = "";
            string keywords = "";
            for (int i = 0; i < args.Length; i++)
            {
                //
                // PARSE FILENAME ARG
                //
                if (args[i] == "-f" || args[i] == "-F")
                {
                    file_name += args[i + 1];
                }

                //
                // PARSE DESCRIPTION ARG
                //
                if (args[i] == "-d" || args[i] == "-D")
                {
                    description += args[i + 1];
                }

                //
                // PARSE KEYWORD ARGS
                //
                if (args[i] == "-k" || args[i] == "-K")
                {
                    keywords += args[i + 1] + " ";
                }
            }

            string[] parsed_arguments = { file_name, description, keywords };
            return parsed_arguments;
        }

        //
        //  Format SEARCH XML Document Args
        //
        static string[][] parseSearchXMLArgs(string[] args)
        {
            List<string> file_name = new List<string> { };
            List<string> description = new List<string> { };
            List<string> keywords = new List<string> { };

            for (int i = 0; i < args.Length; i++)
            {
                //
                // PARSE FILENAME ARG
                //
                if (args[i] == "-f" || args[i] == "-F")
                {
                    //
                    // Separate by spaces, then add to string list
                    //
                    foreach (string arg in args[i + 1].Split())
                    {
                        file_name.Add(arg);
                    }

                }

                //
                // PARSE DESCRIPTION ARG
                //
                if (args[i] == "-d" || args[i] == "-D")
                {
                    //
                    // Split desc by spaces, then add to desc list 
                    //
                    foreach (string arg in args[i + 1].Split())
                    {
                        description.Add(arg);
                    }
                }

                //
                // PARSE KEYWORD ARGS
                //
                if (args[i] == "-k" || args[i] == "-K")
                {
                    //
                    //  Split keywords by spaces, then add to keyword list
                    //
                    foreach (string arg in args[i + 1].Split())
                    {
                        keywords.Add(arg);
                    }
                }
            }

            string[][] parsed_arguments = { file_name.ToArray(), description.ToArray(), keywords.ToArray() };
            return parsed_arguments;
        }

        //
        // This Function handles the task of asking the user for necessary data
        // The generateXML static function is called to generate the file
        //
        static void getUserXMLData(string file_name = "DefaultXML", string description = "Default Description", string[] key_words = null)
        {
            if (key_words == null)
            {
                key_words = new string[] { "None" };
            }

            Console.WriteLine($"CreatinG XML from input {file_name}, {description}, and {key_words}.");

            //
            // GET CURRENT DIR
            //
            string currentDir = Environment.CurrentDirectory;

            //
            // CALL GENERATE XML
            // 
            XElement userFile = generateXML(file_name, description, key_words);
            Console.WriteLine("File Contents: ");
            Console.WriteLine(userFile);

            new XDocument(userFile).Save($"{currentDir}/{file_name}.xml");
            Console.WriteLine($"{Environment.NewLine}New XML document created in location {currentDir}/{file_name}.xml");
        }

        static XElement generateXML(string fileName, string desc, string[] keyWords)
        {
            if (keyWords == null)
            {
                keyWords = new string[] { "None" };
            }

            XElement XML_file = new XElement("FileInfo",
                new XElement("FileName", fileName),
                new XElement("Description", desc),
                new XElement("Keywords",
                Enumerable.Range(0, keyWords.Length).Select(i =>
                new XElement("Keyword",
                    new XAttribute("Number", i), keyWords[i]))
                )
            );

            return XML_file;
        }


        //
        // Search Files in current directory and return list of found names/paths
        // 

        static string[] searchFiles(string[] file_names = null)
        {
            List<string> files = new List<string> { };

            //
            // Catch Null if non params
            //
            if (file_names == null)
            {
                file_names = new string[] { "*" };
            }

            //
            // ITERATE GIVEN FILE NAMES
            //
            foreach (string file_name in file_names)
            {
                Console.WriteLine($"{Environment.NewLine}Searching for {file_name} ...");
                string currentDir = Environment.CurrentDirectory;
                string[] tmp = Directory.GetFiles(currentDir, file_name, SearchOption.AllDirectories);

                //
                // If tmp is poplated, then matching files are found
                //
                if (tmp.Length > 0)
                {
                    Console.WriteLine($"    Found Matching file for Search Input: {file_name}");
                    files.Add(tmp[0]);
                }
            }

            //
            // If results are found, iterate and print them
            //
            if (files.Count > 0)
            {
                Console.WriteLine($"{Environment.NewLine}Files Located From Search:");
                foreach (string file_name in files)
                {
                    Console.WriteLine($"{file_name},{Environment.NewLine}");
                }
            }
            else
            {
                Console.WriteLine($"No files located matching specified FILENAME criteria {Environment.NewLine}");
            }

            string[] final_files = files.ToArray();
            return final_files;
        }

        //
        // Search XML document nodes
        //
        static void searchXMLDescriptionNodes(string[] desc_search_terms)
        {
            //
            // If no description, then skip and return
            //
            if (desc_search_terms.Length <= 0)
            {
                Console.WriteLine($"No files located matching specified DESCRIPTION search criteria {Environment.NewLine}");
                return;
            }

            //
            // GAT ALL FILES IN DIRECTORY
            //
            string[] file_names = Directory.GetFiles(Environment.CurrentDirectory, "*.xml", SearchOption.AllDirectories);

            foreach (string file_name in file_names)
            {
                XDocument doc = XDocument.Load(file_name);
                Console.WriteLine($"{Environment.NewLine}Checking Description Search Criteria in File: {file_name}");

                //
                // SEARCH XML DESCRIPTION NODES
                //
                foreach (var desc_nodes in doc.Descendants("Description"))
                {
                    var textValue = string.Concat(desc_nodes.Nodes().OfType<System.Xml.Linq.XText>().Select(tx => tx.Value));

                    //
                    // CHECK FOR SEARCH TERMS IN DESCRIPTION NODES
                    //
                    foreach (string search_desc in desc_search_terms)
                    {
                        Console.WriteLine($"Searching Term: '{search_desc}'");

                        //
                        // IF MATCH IS FOUND PRINT RESULT INFO
                        //
                        if (textValue.Contains(search_desc))
                        {
                            Console.WriteLine($"    Found Match with Search Term '{search_desc}' in XML File: {file_name}{Environment.NewLine}    " +
                                $"{desc_nodes.Name.ToString()}, {textValue}{Environment.NewLine}");
                        }
                        else
                        {
                            Console.WriteLine($"    No Match Found...");
                        }
                    }
                }

            }

        }

        //
        // SEARCH XML doc keywords
        //
        static void searchXMLKeywordNodes(string[] keyword_search_terms)
        {
            //
            // If no description, then skip and return
            //
            if (keyword_search_terms.Length <= 0)
            {
                Console.WriteLine($"No files located matching specified KEYWORD search criteria {Environment.NewLine}");
                return;
            }

            //
            // GET ALL DIRECTORY FILES
            //
            string[] file_names = Directory.GetFiles(Environment.CurrentDirectory, "*.xml", SearchOption.AllDirectories);

            //
            // ITERATE EACH FILE CHECKING KEYWORDS
            //
            foreach (string file_name in file_names)
            {
                XDocument doc = XDocument.Load(file_name);

                Console.WriteLine($"{Environment.NewLine}Checking Keyword Search Criteria in File: {file_name}");

                //
                // SEARCH XML KEYWORD
                //
                foreach (var keywrd_nodes in doc.Descendants("Keyword"))
                {
                    var textValue = string.Concat(keywrd_nodes.Nodes().OfType<System.Xml.Linq.XText>().Select(tx => tx.Value));

                    //
                    // CHECK EACH KEYWORD AGAINS FILE KEYWORDS
                    //
                    foreach (string keyword in keyword_search_terms)
                    {
                        Console.WriteLine($"Keyword Search: '{keyword}'");

                        //
                        // IF MATCH IS FOUND PRINT RESULT INFO
                        //
                        if (textValue.Contains(keyword))
                        {
                            Console.WriteLine($"    Found Match with Keyword '{textValue}' in XML File: {file_name.Trim()}{Environment.NewLine}    " +
                                $"{keywrd_nodes.Name.ToString()}, {textValue}");
                        }
                        else
                        {
                            Console.WriteLine($"    No Match Found...");
                        }
                    }
                    Console.WriteLine($"{Environment.NewLine}");
                }
            }

        }
    }

}
