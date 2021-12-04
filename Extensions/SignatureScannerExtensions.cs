﻿using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.SourceSplit.Extensions
{
    static class SignatureScannerExtensions
    {
        static public IntPtr ReadCall(this SignatureScanner scanner, IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return IntPtr.Zero;
            return (IntPtr)(scanner.Process.ReadValue<int>(ptr + 0x1) + (int)(ptr + 5));
        }
    }
}
