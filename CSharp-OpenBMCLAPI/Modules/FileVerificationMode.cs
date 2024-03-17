using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    public enum FileVerificationMode
    {
        None,
        Exists,
        SizeOnly,
        Hash
    }
}
