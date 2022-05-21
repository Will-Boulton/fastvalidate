using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using FastValidate.Attributes;
using FastValidate.SourceGen.Validations.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace FastValidate.SourceGen;

//[Generator]
public class FastValidateGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ValidateTypeReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if(context.SyntaxContextReceiver is not ValidateTypeReceiver rec)
            return;

        foreach (var syntaxctx in rec.ReceivedTypeDeclarations)
        {
            var typeDef = (TypeDeclarationSyntax) syntaxctx.Node;
            
            var kind = typeDef.Kind();
            
            if(!HasValidateAttribute(syntaxctx, typeDef.AttributeLists))
                continue;
            
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    break;
                default:
                    ReportUnsupportedType(context, syntaxctx);
                    continue;
            }
            
            var symbol = syntaxctx.SemanticModel.GetSymbolInfo(syntaxctx.Node).Symbol as INamedTypeSymbol;

            if (symbol is null)
            {
                ReportUnknownError(context, syntaxctx);
                continue;
            }    
            
            if(symbol.IsStatic)
            {
                ReportStaticTypeError(context, syntaxctx, symbol);
                continue;
            }

           
            var inRangeMembers = ExtractValidators(syntaxctx, typeDef);

            if (inRangeMembers.Count == 0)
            {
                WarnAttributeDoesNothing(context, syntaxctx);
                continue;
            }
            
            Generate generate; 
            if (TypeDeclaredAsPartial(typeDef.Modifiers))
            {
                generate = Generate.TypeMethods;
            }
            else
            {
                generate = Generate.ExtensionMethods;
            }
        }
        
    }

    private Dictionary<ISymbol, List<INumericValidation>> ExtractValidators(GeneratorSyntaxContext ctx,TypeDeclarationSyntax type)
    {
        Dictionary<ISymbol, List<INumericValidation>> numericValidations = new(SymbolEqualityComparer.Default);
        foreach (var member in type.Members)
        {
            var kind = member.Kind();
            
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(member);
            ITypeSymbol typeSymbol;
            object? value;
            switch (kind)
            {
                case SyntaxKind.FieldDeclaration:
                    var field = member as FieldDeclarationSyntax;
                    var f = symbol as IFieldSymbol;
                    //warn on constant value 
                    //value = f.HasConstantValue

                    typeSymbol = f?.Type;
                    
                    break;
                case SyntaxKind.PropertyDeclaration:
                    //var property = member as PropertyDeclarationSyntax;
                    var p = symbol as IPropertySymbol;
                    typeSymbol = p?.Type;
                    break;
                default:
                    // only validate fields and properties
                    continue;
            }

            var typeIsSpecial = typeSymbol.SpecialType == SpecialType.None;
                
            
            var validations = INumericValidationAttributes(ctx, member, typeSymbol);
            
            if(validations.Any())
                numericValidations[symbol] = validations;
        }

        return numericValidations;
    }

    private List<INumericValidation> INumericValidationAttributes(GeneratorSyntaxContext ctx, MemberDeclarationSyntax member, ITypeSymbol typeSymbol)
    {
        List<INumericValidation> validations = new();
        foreach (var attributeListSyntax in member.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(ctx.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                { continue; }
                
                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();
                
                if (fullName == typeof(GreaterThanAttribute).FullName)
                {
                }
                if (fullName == typeof(LessThanAttribute).FullName)
                {
                }
                if (fullName == typeof(BetweenAttribute).FullName)
                {
             
                }
            }
        }
        return validations;
    }
    
    private bool HasValidateAttribute(GeneratorSyntaxContext ctx, SyntaxList<AttributeListSyntax> attributeListsList)
    {
        foreach (var attributeListSyntax in attributeListsList)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(ctx.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                { continue; }
                
                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();
                
                if (fullName == typeof(ValidateAttribute).FullName)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private bool CollectNumericValidations(GeneratorSyntaxContext ctx, SyntaxList<AttributeListSyntax> attributeListsList)
    {
        foreach (var attributeListSyntax in attributeListsList)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(ctx.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                { continue; }
                
                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();
                
                if (fullName == typeof(ValidateAttribute).FullName)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    
    private enum Generate
    {
        ExtensionMethods,
        TypeMethods
    }
    
    private static void WarnAttributeDoesNothing(GeneratorExecutionContext context, GeneratorSyntaxContext ctx)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.UnsupportedTypeDeclaration_Descriptor,
                ctx.Node.GetLocation())
        );
    }
    
    private static void ReportUnsupportedType(GeneratorExecutionContext context, GeneratorSyntaxContext ctx)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.UnsupportedTypeDeclaration_Descriptor,
                ctx.Node.GetLocation())
        );
    }
    
    private static void ReportUnknownError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.Unknown_Descriptor,
                ctx.Node.GetLocation())
        );
    }

    private static void ReportStaticTypeError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx,
        INamedTypeSymbol symbol)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Static_Type_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name)
        );
    }

    private bool TypeDeclaredAsPartial(SyntaxTokenList modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
                return true;
        }

        return false;
    }
    
    
}
internal class ValidateTypeReceiver : ISyntaxContextReceiver
{
    private readonly List<GeneratorSyntaxContext> _receivedTypeDeclarations = new ();

    public IReadOnlyList<GeneratorSyntaxContext> ReceivedTypeDeclarations => _receivedTypeDeclarations;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is TypeDeclarationSyntax { AttributeLists.Count: > 0 })
        {
            _receivedTypeDeclarations.Add(context);
        }
    }
}