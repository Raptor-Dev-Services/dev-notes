using BenchmarkDotNet.Running;

// Uso: dotnet run -c Release -- --filter "*ContarPalabras*"
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
