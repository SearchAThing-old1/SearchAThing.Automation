#region SearchAThing.Automation, Copyright(C) 2016 Lorenzo Delana, License under MIT
/*
* The MIT License(MIT)
* Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
*
* Permission is hereby granted, free of charge, to any person obtaining a
* copy of this software and associated documentation files (the "Software"),
* to deal in the Software without restriction, including without limitation
* the rights to use, copy, modify, merge, publish, distribute, sublicense,
* and/or sell copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
* DEALINGS IN THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SearchAThing.Automation.Example
{
    class Program
    {

        static void Main(string[] args)
        {
            test2();
        }

        static void test1()
        {
            var str =
@"
using System;
using System.Windows;
public class MyClass
{
    
    /// <summary>
    /// test using integer
    /// </summary>
    public void Test(int x) { }    

    public void CompleteMe()
    {
        /**/ MessageBox.
    }
}";

            var off = str.IndexOf("/**/ MessageBox.") + "/**/ MessageBox.".Length;

            var a = typeof(System.Windows.MessageBox);

            var assemblies = new[]
            {
                a.Assembly
            };

            var task = str.AutoComplete(off, assemblies);

            foreach (var x in task.Result)
            {
                Console.WriteLine(x);
            }
        }

        static void test2()
        {
            var str =
@"
using SearchAThing.Automation.Example;
public class MyClass
{       
    public void CompleteMe()
    {
        /**/ MyUtils.
    }
}";

            var off = str.IndexOf("/**/ MyUtils.") + "/**/ MyUtils.".Length;

            var a = typeof(System.Windows.MessageBox);

            var assemblies = new[]
            {
                Assembly.GetExecutingAssembly()
            };
            
            var task = str.AutoComplete(off, assemblies);

            foreach (var x in task.Result)
            {
                Console.WriteLine(x);
            }
        }

    }

    public static class MyUtils
    {

        public static int Sum(int x, int y)
        {
            return x + y;
        }

        public static int Sum(int x, int y, int z)
        {
            return x + y + z;
        }

    }

}
