using System.Diagnostics;
using System.Reflection;

namespace PortaleFatture.BE.Api.Infrastructure;

public static class ModuleExtensions
{
    public static IEndpointRouteBuilder Map(this IEndpointRouteBuilder app, Type moduleType)
    {
        //Guard.Against.Null(app, nameof(app));
        //Guard.Against.Null(moduleType, nameof(moduleType));

        foreach (var method in moduleType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            app.Map(method, moduleType);
        }

        return app;
    }

    public static IEndpointConventionBuilder? Map(this IEndpointRouteBuilder app, MethodInfo method, Type moduleType,
        string? routePrefix = null)
    {
        //Guard.Against.Null(app, nameof(app));
        //Guard.Against.Null(method, nameof(method));
        //Guard.Against.Null(moduleType, nameof(moduleType));

        var mapAttr = method.GetCustomAttribute<MapAttribute>();
        if (mapAttr is null)
        {
            return null;
        }

        var route = mapAttr.Route ?? method.Name.ToLowerInvariant();
        if (!string.IsNullOrEmpty(routePrefix))
        {
            route = $"{routePrefix}/{route}";
        }

        var options = new RequestDelegateFactoryOptions { ThrowOnBadRequest = true };
        var metaData = RequestDelegateFactory.InferMetadata(method, options);
        var delegateResult = RequestDelegateFactory.Create(method,
            ctx => ctx.RequestServices.GetRequiredService(moduleType), options, metaData);
        return app.Map(mapAttr.Method, route, delegateResult.RequestDelegate);
    }

    [DebuggerStepThrough]
    public static IEndpointConventionBuilder Map(this IEndpointRouteBuilder app, MapMethod method, string route,
        RequestDelegate mapDelegate)
    {
        //Guard.Against.NullOrEmpty(route, nameof(route));
        //Guard.Against.Null(mapDelegate, nameof(mapDelegate));

        return method switch
        {
            MapMethod.Get => app.MapGet(route, mapDelegate),
            MapMethod.Post => app.MapPost(route, mapDelegate),
            MapMethod.Put => app.MapPut(route, mapDelegate),
            MapMethod.Delete => app.MapDelete(route, mapDelegate),
            MapMethod.Patch => app.MapPatch(route, mapDelegate),
            _ => throw new NotSupportedException($"Not supported MapMethod {method}")
        };
    }
}