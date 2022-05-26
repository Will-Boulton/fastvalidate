using System;
using System.Linq;
using FastValidate.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FastValidate.Test;

[GenerateValidateMethod]
public partial class ValidateMe
{
    public ValidateMe(int number, int number2)
    {
        Number = number;
        Number2 = number2;
    }

    [Validate.Between(5,100)]
    public int Number { get; set; }
    
    [Validate.GreaterThan(0)]
    public int Number2 { get; set; }
}

public class UnitTest1
{
    [Fact]
    public void SourceGenerateAndTest()
    {
       IFastValidatable i = new ValidateMe(6, 1);
       Assert.True(i.Validate());
       
       i = new ValidateMe(5, 1);
       Assert.False(i.Validate());
    }
    
    [Fact]
    public void CompileAndTest()
    {
        
        var source = @"
using FastValidate.Attributes;
using System;
namespace A;

[GenerateValidateMethod]
public partial class Car
{
    [Validate.GreaterThan(3), Validate.LessThan(5)]
    public int NWheels { get; }
}
";
        var syntax = CSharpSyntaxTree.ParseText(source);
        
        var compilation = CSharpCompilation.Create(
            assemblyName: "Test",
            syntaxTrees: new[] { syntax },
            references: new [] 
            { 
                MetadataReference.CreateFromFile(typeof(GenerateValidateMethodAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "netstandard").Location),
                MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "System.Runtime").Location)
            }); 
        
        var generator = new FastValidateGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = (CSharpGeneratorDriver) driver.RunGenerators(compilation);

        var rr = driver.GetRunResult();
    }
}