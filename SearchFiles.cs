using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace P2P_Network_Launcher
{
    class SearchFiles
    {
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
