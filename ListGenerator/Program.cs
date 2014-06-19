using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ListGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            try {
                if (args.Length == 0)
                {
                    new ModListGenerator2().Refresh();
                }
                else
                {
                    var action = args[0];
                    if (action == "add" && args.Length == 3)
                    {
                        var identifier = args[1];
                        var url = args[2];
                        new ModListGenerator2().Add(identifier, url);
                    }
                    else if (action == "remove" && args.Length == 2)
                    {
                        var identifier = args[1];
                        new ModListGenerator2().Remove(identifier);
                    }
                    else
                    {
                        Usage();
                        return 1;
                    }
                }

                Console.WriteLine();
                new ModListGenerator().Run();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 2;
            }

            return 0;
        }

        static void Usage()
        {
            Console.WriteLine("usage: listgenerator [add identifier url|remove identifier]");
        }
    }
}
