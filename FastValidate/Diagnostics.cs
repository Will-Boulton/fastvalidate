using FastValidate.Attributes;
using Microsoft.CodeAnalysis;

namespace FastValidate;

internal static partial class Diagnostics
{
    internal static class Categories
    {
        public const string FastValidate = "FastValidate.CodeGen";
    }

    private const string Not_Public_Format = "type '{0}.{1}' must be declared as public";
    private const string Not_Public_Title = "type must be public";
    private const string Not_Public_Id = "ERR-FV-001";
    
    public static DiagnosticDescriptor Not_Public_Descriptor = 
        new(Not_Public_Id,
            Not_Public_Title,
            Not_Public_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);    

    private const string Not_Public_ContainingType_Format = "containing type '{0}.{2}' of type '{0}.1' must be declared as public";
    private const string Not_Public_ContainingType_Id = "ERR-FV-001b";
    
    public static DiagnosticDescriptor Not_Public_ContainingType_Descriptor = 
        new(Not_Public_ContainingType_Id,
            Not_Public_Title,
            Not_Public_ContainingType_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);   
    
    private const string Not_Partial_Format = "type '{0}.{1}' must be declared as partial";
    private const string Not_Partial_Title = "type must be partial";
    private const string Not_Partial_Id = "ERR-FV-002";
    
    public static DiagnosticDescriptor Not_Partial_Descriptor = 
        new(Not_Partial_Id,
            Not_Partial_Title,
            Not_Partial_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    private const string Not_Partial_Containing_Type_Format = "containing type '{0}.{2}' of type '{0}.1' must be declared as partial";
    private const string Not_Partial_Containing_Type_Id = "ERR-FV-002b";
    
    public static DiagnosticDescriptor Not_Partial_Containing_Type_Descriptor = 
        new(Not_Partial_Containing_Type_Id,
            Not_Partial_Title,
            Not_Partial_Containing_Type_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    private const string Unknown_Format = "unknown error occurred while generating. symbol type '{0}'";
    private const string Unknown_Title = "unknown error ";
    private const string Unknown_Id = "ERR-FV-999";
    
    public static DiagnosticDescriptor Unknown_Descriptor = 
        new(Unknown_Id,
            Unknown_Title,
            Unknown_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    // 0 -> namespace
    // 1 -> type name
    private const string Static_Type_Format = "cannot generate validations for static type {0}.{1}";
    private const string Static_Type_Title = "cannot generate validations for static type";
    private const string Static_Type_Id = "ERR-FV-003";
    
    public static DiagnosticDescriptor Static_Type_Descriptor = 
        new(Static_Type_Id,
            Static_Type_Title,
            Static_Type_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);


    private const string UnsupportedTypeDeclaration_Format = "unsupported type of declaration, FastValidate currently only supports class, struct, record, and record struct types";
    private const string UnsupportedTypeDeclaration_Title = "unsupported type declaration";
    private const string UnsupportedTypeDeclaration_Id = "ERR-FV-004";

    public static DiagnosticDescriptor UnsupportedTypeDeclaration_Descriptor = 
        new(UnsupportedTypeDeclaration_Id,
            UnsupportedTypeDeclaration_Title,
            UnsupportedTypeDeclaration_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Error,
            true);
    
    private const string NoEffect_Format = "Validate attribute will have no effect as no members have defined validators";
    private const string NoEffect_Title = "unsupported type declaration";
    private const string NoEffect_Id = "WARN-FV-001";

    public static DiagnosticDescriptor NoEffect_Descriptor = 
        new(NoEffect_Id,
            NoEffect_Title,
            NoEffect_Format,
            Categories.FastValidate,
            DiagnosticSeverity.Warning,
            true);
}