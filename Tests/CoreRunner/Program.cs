using System;
using EchoFactory.Tests;
internal static class Program { private static int Main() { try { Console.WriteLine(CoreChecks.Run()); Console.WriteLine("PASS: " + StrategyChecks.Run() + " strategic assertions."); return 0; } catch (Exception e) { Console.Error.WriteLine(e); return 1; } } }
