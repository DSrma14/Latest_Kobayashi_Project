using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spatial.CGM;

namespace IopCgmSample
{
    public class ArgParser1
    {
        public Dictionary<string, string> argMap;
        bool mapSuccess = true;
        public ArgParser1(string[] args, string[] permissibleArgs)
        {
           if (args.Length != 0)
            {
                argMap = new Dictionary<string, string>();
                int pos = 0;
                while (pos < args.Length)
                {
                    string key = args[pos];
                    string value = "";
                    if ((pos + 1) < args.Length)
                        value = args[pos + 1];
                    argMap.Add(key, value);
                    pos = pos + 2;
                }

                List<string> keyList = new List<string>(argMap.Keys);
                foreach (string key in keyList)
                {
                    bool is_allowed = permissibleArgs.Contains(key);
                    if (!is_allowed)
                    {
                        mapSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                mapSuccess = false;
            }
        }
        public bool isSuccess()
        {
            return mapSuccess;
        }

        public bool GetArgValue(string arg, out string value)
        {
            bool success = argMap.TryGetValue(arg, out value);
            return success;
        }
    }
}


