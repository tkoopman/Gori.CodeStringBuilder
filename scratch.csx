using Gori.CodeStringBuilder;
var sb = CodeStringBuilder.CreateCSharpBuilder(fullAuto: true);
sb.WriteLine("namespace MyApp")
  .WriteLine("{")
  .WriteLine("public class MyClass")
  .WriteLine("{")
  .WriteLine("public void Method()")
  .WriteLine("{")
  .WriteLine("// body")
  .WriteLine("}")
  .WriteLine("}")
  .WriteLine("}");
Console.Write(sb.ToString().Replace("\r\n", "\\r\\n\n").Replace("\n", "\\n\n"));