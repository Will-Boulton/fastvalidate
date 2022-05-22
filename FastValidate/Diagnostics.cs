using Microsoft.CodeAnalysis;

namespace FastValidate;

internal static partial class Diagnostics
{
    internal static class Categories
    {
        public const string FastValidate = "FastValidate.CodeGen";
    }

    private const string Unknown_Format = "unknown error occurred while generating. symbol type '{0}'";
    private const string Unknown_Title = "unknown error ";
    private const string Unknown_Id = "FV002E";
    
    public static DiagnosticDescriptor Unknown_Descriptor = 
        new(Unknown_Id,
            Unknown_Title,
            Unknown_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    // 0 -> namespace
    // 1 -> type name
    private const string Static_Type_Format = "cannot generate validations for static type";
    private const string Static_Type_Title = "cannot generate validations for static type {0}.{1}";
    private const string Static_Type_Id = "FV003E";
    
    public static DiagnosticDescriptor Static_Type_Descriptor = 
        new(Static_Type_Id,
            Static_Type_Title,
            Static_Type_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    
    private const string UnsupportedTypeDeclaration_Format = "unsupported type of declaration, FastValidate currently only supports class, struct, interface, record, and record struct types";
    private const string UnsupportedTypeDeclaration_Title = "unsupported type declaration";
    private const string UnsupportedTypeDeclaration_Id = "FV004E";

    public static DiagnosticDescriptor UnsupportedTypeDeclaration_Descriptor = 
        new(UnsupportedTypeDeclaration_Id,
            UnsupportedTypeDeclaration_Title,
            UnsupportedTypeDeclaration_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    private const string NoEffect_Format = "Validate attribute will have no effect as no members have defined validators";
    private const string NoEffect_Title = "unsupported type declaration";
    private const string NoEffect_Id = "FV001W";

    public static DiagnosticDescriptor NoEffect_Descriptor = 
        new(NoEffect_Id,
            NoEffect_Title,
            NoEffect_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Warning,
            true);
}