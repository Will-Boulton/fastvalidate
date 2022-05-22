using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FastValidate.Attributes;
using FastValidate.Validations.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace FastValidate;

[Generator]
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

        
#if DEBUG
        SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
        Dictionary<string, INamedTypeSymbol> _attributeLookup = new()
        {
            {typeof(GreaterThanAttribute).FullName,context.Compilation.GetTypeByMetadataName(typeof(GreaterThanAttribute).FullName)},
            {typeof(LessThanAttribute).FullName,context.Compilation.GetTypeByMetadataName(typeof(LessThanAttribute).FullName)},
            {typeof(BetweenAttribute).FullName,context.Compilation.GetTypeByMetadataName(typeof(BetweenAttribute).FullName)},
        };

        int i = 0;
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

            var symbol_ = syntaxctx.SemanticModel.GetDeclaredSymbol(syntaxctx.Node); 
            var symbol = symbol_ as INamedTypeSymbol;
            if (symbol is null)
            {
                ReportUnknownError(context, syntaxctx,symbol_);
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
            context.AddSource($"{i++}.g.cs","//");
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
            var validations = INumericValidationAttributes(ctx, symbol);
            
            if(validations.Any())
                numericValidations[symbol] = validations;
        }

        return numericValidations;
    }

    private List<INumericValidation> INumericValidationAttributes(GeneratorSyntaxContext ctx, ISymbol symbol)
    {
        List<INumericValidation> validations = new();
        foreach (var attribute in symbol.GetAttributes())
        {
            
            var fullName = attribute.AttributeClass?.MetadataName;
            
            if (fullName == typeof(GreaterThanAttribute).Name)
            {
                if (TryGetBoundCheck(symbol, attribute, out var bound))
                {
                    bound.IsGreaterThanCheck = true;
                    validations.Add(bound);
                }
                // BoundValidation.GreaterThan(member,)
            }
            if (fullName == typeof(LessThanAttribute).Name)
            {
                if (TryGetBoundCheck(symbol, attribute, out var bound))
                {
                    bound.IsGreaterThanCheck = false;
                    validations.Add(bound);
                }
                // BoundValidation.LessThan(member,);
            }
            if (fullName == typeof(BetweenAttribute).Name)
            {
                var p1 = attribute.ConstructorArguments[0];
                var p1Type = p1.Type;
                var p2 = attribute.ConstructorArguments[1];
                var p2Type = p2.Type;

                if (IsParamTypeOk(p1Type) && IsParamTypeOk(p2Type))
                {
                    validations.Add(new RangeValidation(symbol.Name,p1.Value,p2.Value));
                }
            }
            
        }
        return validations;
    }

    private bool IsParamTypeOk(ITypeSymbol parameterType)
    {
        return parameterType.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_Double;
    }

    private bool TryGetBoundCheck(ISymbol symbol, AttributeData attribute, out BoundValidation boundValidation)
    {
        boundValidation = null;
        var parameter = attribute.ConstructorArguments[0];
        var parameterType = parameter.Type;

        if (!IsParamTypeOk(parameterType))
            return false;
        
        boundValidation = new BoundValidation(symbol.Name, parameter.Value);
        return true;

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

    
    private enum Generate
    {
        ExtensionMethods,
        TypeMethods
    }
    
    private static void WarnAttributeDoesNothing(GeneratorExecutionContext context, GeneratorSyntaxContext ctx)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.NoEffect_Descriptor,
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
    
    private static void ReportUnknownError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, ISymbol symbol)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.Unknown_Descriptor,
                ctx.Node.GetLocation(),
                symbol.Kind)
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