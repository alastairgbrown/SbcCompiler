using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcLibrary
{

    [Noclass]
    public class Snippet
    {
        public Action<Compiler, Config> Action { get; }
        public Snippet(Action<Compiler, Config> action) => Action = action;
    }
}
