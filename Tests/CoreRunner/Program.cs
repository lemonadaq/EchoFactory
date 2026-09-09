using System;
using EchoFactory.Tests;
internal static class Program { private static int Main() { try { Console.WriteLine(CoreChecks.Run()); return 0; } catch (Exception e) { Console.Error.WriteLine(e); return 1; } } }
