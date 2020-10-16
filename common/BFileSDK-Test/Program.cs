using Sandbox.Tests;
using System;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            TestFileFormat.SimpleReadWriteTest(true);
            TestFileFormat.SimpleReadWriteTest(false);
        }
    }
}
