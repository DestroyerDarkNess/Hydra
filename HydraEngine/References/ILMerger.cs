
using ILMerging;
using System;
using System.Collections.Generic;

namespace HydraEngine.References
{
    public class ILMerger
    {
        public Exception Errors { get; set; } = new Exception("Undefined");
        public bool Ouput { get; set; } = true;

        public bool MergeAssemblies(string Original, List<string> dllModules, string output)
        {
            try
            {

                List<string> ListArg = new List<string>();

                string target = string.Empty;

                if (Original.ToLower().EndsWith(".exe"))
                {
                    target = "/target:winexe";
                }
                else
                {
                    target = "/target:dll";
                }
                ListArg.Add(target);
                ListArg.Add("/log");
                ListArg.Add($"/out:{output}");
                ListArg.Add(Original);

                foreach (string str in dllModules)
                {
                    //Console.WriteLine($"Merging {str}");
                    ListArg.Add(str);
                }

                ILMerge.Main(ListArg.ToArray());
                if (!Ouput) Console.Clear();
                return true;

            }
            catch (Exception ex)
            {
                Errors = ex;
                return false;
            }
        }


    }
}
