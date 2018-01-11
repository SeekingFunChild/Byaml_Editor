using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByamlEdit
{
    class CustomItem
    {
            private string _text = null;

            private ByamlNodeType _value = 0;

            public string Text
            {
                get
                {

                    return this._text;
                }
                set
                {

                    this._text = value;
                }
            }

            public ByamlNodeType Value
            {
                get
                {

                    return this._value;
                }
                set
                {

                    this._value = value;
                }
            }

            public override string ToString()
            {

                return this._text;
            }
        }
}
