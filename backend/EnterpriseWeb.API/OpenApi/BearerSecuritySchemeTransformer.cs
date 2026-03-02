namespace EnterpriseWeb.API.OpenApi;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Document transformer ที่เพิ่ม JWT Bearer security scheme ใน OpenAPI spec
/// และเพิ่ม security requirement ให้ทุก endpoint (ยกเว้น /api/auth/login)
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == "Bearer"))
            return;

        // 1. ลงทะเบียน security scheme ระดับ document
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "ใส่ JWT token ที่ได้จาก POST /api/auth/login"
            }
        };

        // 2. เพิ่ม security requirement ให้ทุก operation ยกเว้น /api/auth/login
        foreach (var (path, pathItem) in document.Paths ?? [])
        {
            // auth/login ไม่ต้องการ token
            if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var operation in pathItem.Operations!.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
