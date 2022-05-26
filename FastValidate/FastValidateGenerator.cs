using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastValidate.Attributes;
using FastValidate.Validations;
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

        if (context.SyntaxContextReceiver is not ValidateTypeReceiver rec)
            return;

        var attrType = context.Compilation.GetTypeByMetadataName(typeof(GenerateValidateMethodAttribute).FullName)!;
        var interfaceType = context.Compilation.GetTypeByMetadataName(typeof(IFastValidatable).FullName)!;

        foreach (var syntaxctx in rec.CandidateTypes)
        {
            var typeDef = (TypeDeclarationSyntax) syntaxctx.Node;

            var kind = typeDef.Kind();

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

            var declaredSymbol = syntaxctx.SemanticModel.GetDeclaredSymbol(syntaxctx.Node);
            if (declaredSymbol is not INamedTypeSymbol symbol)
            {
                ReportUnknownError(context, syntaxctx, declaredSymbol);
                continue;
            }


            var labels = ExtractLabels(symbol, attrType, interfaceType);

            if (labels is Labels.None)
            {
                continue;
            }

            if (ShouldSkipDeclaration(context, symbol, syntaxctx, typeDef))
            {
                continue;
            }

            if (!TryGetTypeHierarchy(context, syntaxctx, symbol, out var reverseHierarchy))
            {
                continue;
            }

            if (!AreMembersCompatible(symbol, out var existingDeclaration))
            {
                continue;
            }

            var makeValidateMethodPartial = existingDeclaration is not null;

            var validators = ExtractValidators(syntaxctx, typeDef);

            if (validators.Count == 0)
            {
                WarnAttributeDoesNothing(context, syntaxctx);
            }

            var sourceBuilder = new StringBuilder();


            sourceBuilder.Append("namespace ").Append(symbol.ContainingNamespace).AppendLine(";").AppendLine();


            var indentation = 1;
            foreach (var parentType in reverseHierarchy)
            {
                sourceBuilder.Append(' ', indentation * 4).Append("public partial").Append(TypeSignature(parentType));
                if (parentType.Equals(symbol, SymbolEqualityComparer.Default))
                {
                    sourceBuilder.Append(" : ").Append(nameof(IFastValidatable));
                }
                sourceBuilder.AppendLine();
                sourceBuilder.Append(' ', indentation * 4).AppendLine("{");
                indentation++;
            }

            var methodSource = GenerateMethodSourceCode(validators, makeValidateMethodPartial);

            var t = typeDef
                .WithAttributeLists(new SyntaxList<AttributeListSyntax>())
                .WithMembers(
                    new SyntaxList<MemberDeclarationSyntax>(
                        SyntaxFactory.ParseMemberDeclaration(methodSource.ToString())
                    ))
                .WithBaseList(
                    SyntaxFactory.BaseList(
                        SyntaxFactory.SeparatedList<BaseTypeSyntax>(
                            new[]
                            {
                                SyntaxFactory.SimpleBaseType(
                                    SyntaxFactory.ParseTypeName(nameof(IFastValidatable))
                                )

                            }
                        )
                    )
                );

            var ns = (BaseNamespaceDeclarationSyntax) symbol.ContainingNamespace.DeclaringSyntaxReferences
                .First()
                .GetSyntax();

            var dec = (SyntaxNode) ns.WithMembers(
                new SyntaxList<MemberDeclarationSyntax>(t));

            while (dec.Parent is BaseNamespaceDeclarationSyntax p)
            {
                dec = p.WithMembers(
                    new SyntaxList<MemberDeclarationSyntax>((BaseNamespaceDeclarationSyntax) dec)
                );
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .WithLeadingTrivia(SyntaxFactory.Trivia(
                    SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true)))
                .WithUsings(
                    SyntaxFactory.List(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(typeof(GenerateValidateMethodAttribute).Namespace)) }))
                .WithMembers(
                    SyntaxFactory.List(new[] { dec }))
                .NormalizeWhitespace();
            
            context.AddSource($"{symbol.Name}.g.cs", compilationUnit.GetText(Encoding.UTF8));
        }

    }


    private static string TypeSignature(INamedTypeSymbol k)
    {
        var btd = (TypeDeclarationSyntax) k.DeclaringSyntaxReferences.First().GetSyntax();
        var kw = btd.Keyword.ValueText;
        var id = btd.Identifier.ToString() + btd.TypeParameterList;
        var constraints = btd.ConstraintClauses.ToString();

        return $"{kw} {id} {constraints}";
    }
    private static StringBuilder GenerateMethodSourceCode(Dictionary<ISymbol, List<IMemberValidation>> validators, bool makeValidateMethodPartial)
    {
        var methodSource = new StringBuilder();

        if (makeValidateMethodPartial)
            methodSource.Append("partial ");

        methodSource.Append("bool ");
        methodSource.Append(nameof(IFastValidatable)).Append('.').Append(nameof(IFastValidatable.Validate)).AppendLine("()");
        methodSource.Append("=>");

        if (validators.Count > 0)
        {
            List<string> validationSourceCode = new();

            foreach (var nVal in validators.Values.SelectMany(x => x).OrderBy(x => x.FuzzyOrdinal))
            {
                validationSourceCode.Add(nVal.SourceString);
            }


            methodSource.Append(string.Join(" && ", validationSourceCode));
        }
        else
        {
            methodSource.Append("true");
        }
        return methodSource.Append(";");
    }

    private static bool AreMembersCompatible(INamedTypeSymbol typeSymbol, out IMethodSymbol? existingMethod)
    {
        existingMethod = null;
        var validateMembers = typeSymbol.GetMembers(nameof(IFastValidatable.Validate));

        foreach (var validateMember in validateMembers)
        {
            if (validateMember is IMethodSymbol method)
            {
                if (!IsCompatible(method, out var isOverload))
                    return false;

                if (isOverload is true)
                {
                    // ignore
                    continue;
                }

                existingMethod = method;
            }
        }

        return true;
    }
    private static bool IsCompatible(IMethodSymbol method, out bool? isOverload)
    {
        isOverload = null;
        // must be of the signature public x bool Validate(y)

        // x can be partial or not
        // if there are any parameters (y) then the return type must match (bool) to be a valid overload
        if (!IsPublic(method) || method.ReturnType.SpecialType != SpecialType.System_Boolean)
        {
            if (method.Parameters.Length > 0)
            {
                isOverload = true;
                return true;
            }
            if (!IsPartial(method))
                return false;
        }

        // check all declarations to see if there is an implementation
        foreach (var syntax in method.DeclaringSyntaxReferences)
        {
            var dec = (MethodDeclarationSyntax) syntax.GetSyntax();
            if (dec is not { ExpressionBody: null, Body: null })
                return false;
        }
        return true;
    }

    private static bool ShouldSkipDeclaration(GeneratorExecutionContext context, INamedTypeSymbol symbol, GeneratorSyntaxContext syntaxctx, TypeDeclarationSyntax typeDef)
    {

        if (symbol.IsStatic)
        {
            ReportStaticTypeError(context, syntaxctx, symbol);
            return true;
        }

        if (!IsPublic(symbol))
        {
            ReportNotPublicError(context, syntaxctx, symbol);
            return true;
        }

        if (!IsPartial(typeDef))
        {
            ReportNotPartialError(context, syntaxctx, symbol);
            return true;
        }

        return false;
    }

    private bool TryGetTypeHierarchy(GeneratorExecutionContext ctx, GeneratorSyntaxContext syntaxContext, INamedTypeSymbol thisType, out LinkedList<INamedTypeSymbol> reversedTypeHierarchy)
    {
        reversedTypeHierarchy = null;
        LinkedList<INamedTypeSymbol> parentTypes = new();
        var type = thisType;
        while (type.ContainingType is not null)
        {
            type = type.ContainingType;
            parentTypes.AddFirst(type);
            if (!IsPartial(type))
            {
                ReportNotPartialContainingTypeError(ctx, syntaxContext, thisType, type);
                return false;
            }
        }
        reversedTypeHierarchy = parentTypes;
        return true;
    }

    private static bool IsPublic(ISymbol thisType)
        => (thisType.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public;

    private static bool IsPartial(ISymbol thisType)
    {
        foreach (var syntaxReference in thisType.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax() as MemberDeclarationSyntax;
            if (IsPartial(syntax))
                return true;
        }
        return false;
    }

    private static bool IsPartial(MemberDeclarationSyntax typeDef)
    {
        foreach (var m in typeDef.Modifiers)
        {
            if (m.IsKind(SyntaxKind.PartialKeyword))
                return true;
        }
        return false;
    }

    private Labels ExtractLabels(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeTypeSymbol, INamedTypeSymbol interfaceTypeSymbol)
    {
        var hasValidate = HasValidateAttribute(typeSymbol, attributeTypeSymbol);
        var hasInterface = HasInterface(typeSymbol, interfaceTypeSymbol);

        return (hasValidate, hasInterface) switch
        {
            (true, true) => Labels.Both,
            (true, false) => Labels.Attribute,
            (false, true) => Labels.Interface,
            (false, false) => Labels.None
        };
    }

    private bool HasInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceTypeSymbol)
    {
        return typeSymbol.Interfaces.Any(i => i.Equals(interfaceTypeSymbol, SymbolEqualityComparer.Default));
    }

    private bool HasValidateAttribute(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeTypeSymbol)
    {
        var attrs = typeSymbol.GetAttributes();
        return attrs.Any(a => a.AttributeClass?.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default) ?? false);
    }


    private Dictionary<ISymbol, List<IMemberValidation>> ExtractValidators(GeneratorSyntaxContext ctx, TypeDeclarationSyntax type)
    {
        Dictionary<ISymbol, List<IMemberValidation>> numericValidations = new(SymbolEqualityComparer.Default);
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

            if (validations.Any())
                numericValidations[symbol] = validations;
        }

        return numericValidations;
    }

    private List<IMemberValidation> INumericValidationAttributes(GeneratorSyntaxContext ctx, ISymbol symbol)
    {
        List<IMemberValidation> validations = new();
        foreach (var attribute in symbol.GetAttributes())
        {

            var fullName = attribute.AttributeClass?.MetadataName;

            if (fullName == typeof(Validate.GreaterThanAttribute).Name)
            {
                if (TryGetBoundCheck(symbol, attribute, out var bound))
                {
                    bound.IsGreaterThanCheck = true;
                    validations.Add(bound);
                }
                // BoundValidation.GreaterThan(member,)
            }
            if (fullName == typeof(Validate.LessThanAttribute).Name)
            {
                if (TryGetBoundCheck(symbol, attribute, out var bound))
                {
                    bound.IsGreaterThanCheck = false;
                    validations.Add(bound);
                }
                // BoundValidation.LessThan(member,);
            }
            if (fullName == typeof(Validate.BetweenAttribute).Name)
            {
                var p1 = attribute.ConstructorArguments[0];
                var p1Type = p1.Type;
                var p2 = attribute.ConstructorArguments[1];
                var p2Type = p2.Type;

                if (IsParamTypeOk(p1Type) && IsParamTypeOk(p2Type))
                {
                    validations.Add(new RangeValidation(symbol.Name, p1.Value, p2.Value));
                }
            }

        }
        return validations;
    }

    private bool IsParamTypeOk(ITypeSymbol parameterType)
        => parameterType.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_Double;

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

    #region Diagnostics

    private static void WarnAttributeDoesNothing(GeneratorExecutionContext context, GeneratorSyntaxContext ctx)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.NoEffect_Descriptor,
                ctx.Node.GetLocation())
        );
    }


    private static void ReportNotPartialError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Not_Partial_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name)
        );
    }

    private static void ReportNotPublicError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Not_Public_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name)
        );
    }

    private static void ReportNotPublicContainingTypeError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol, INamedTypeSymbol containingType)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Not_Public_ContainingType_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name,
                containingType.Name)
        );
    }

    private static void ReportNotPartialContainingTypeError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol, INamedTypeSymbol containingType)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Not_Partial_Containing_Type_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name,
                containingType.Name)
        );
    }

    private static void ReportValidateAlreadyExists(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(Diagnostics.Not_Partial_Descriptor,
                ctx.Node.GetLocation(),
                symbol.ContainingNamespace.ToString(),
                symbol.Name)
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

    private static void ReportStaticTypeError(GeneratorExecutionContext context, GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
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

    #endregion

}
[Flags]
internal enum Labels : byte
{
    None = 0,
    Attribute = 1,
    Interface = 2,
    Both = Attribute | Interface
}