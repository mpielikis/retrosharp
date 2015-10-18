using System;
using System.Collections.Generic;
using System.Text;

namespace Solution1
{
    class A<T>
    {
        public T GenericProperty { get; set; }
        public T GenericMethod(T arg) { return arg; }
    }
}
