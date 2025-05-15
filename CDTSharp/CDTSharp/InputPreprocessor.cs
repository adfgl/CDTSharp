using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class InputPreprocessor
    {
        CDTInput _input;

        public InputPreprocessor(CDTInput input)
        {
            _input = input;
        }

        public CDTInput Input => _input;
    }
}
