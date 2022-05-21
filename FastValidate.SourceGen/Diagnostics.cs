using Microsoft.CodeAnalysis;

namespace FastValidate.SourceGen;

internal static partial class Diagnostics
{
    internal static class Categories
    {
        public const string FastValidate = "FastValidate.CodeGen";
    }
    
    public const string Unknown_Format = "unknown error";
    public const string Unknown_Title = "unknown error occurred while generating";
    public const string Unknown_Id = "FV002E";
    
    public static DiagnosticDescriptor Unknown_Descriptor = 
        new(Unknown_Id,
            Unknown_Title,
            Unknown_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    // 0 -> namespace
    // 1 -> type name
    public const string Static_Type_Format = "type cannot be static";
    public const string Static_Type_Title = "cannot generate validations for static type {0}.{1}";
    public const string Static_Type_Id = "FV003E";
    
    public static DiagnosticDescriptor Static_Type_Descriptor = 
        new(Static_Type_Id,
            Static_Type_Title,
            Static_Type_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    
    public const string UnsupportedTypeDeclaration_Format = "unsupported type of declaration, FastValidate currently only supports class, struct, interface, record, and record struct types";
    public const string UnsupportedTypeDeclaration_Title = "unsupported type declaration";
    public const string UnsupportedTypeDeclaration_Id = "FV004E";

    public static DiagnosticDescriptor UnsupportedTypeDeclaration_Descriptor = 
        new(UnsupportedTypeDeclaration_Id,
            UnsupportedTypeDeclaration_Title,
            UnsupportedTypeDeclaration_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    public const string NoEffect_Format = "Validate attribute will have no effect as no members have defined validators";
    public const string NoEffect_Title = "unsupported type declaration";
    public const string NoEffect_Id = "FV004E";

    public static DiagnosticDescriptor NoEffect_Descriptor = 
        new(NoEffect_Id,
            NoEffect_Title,
            NoEffect_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
}