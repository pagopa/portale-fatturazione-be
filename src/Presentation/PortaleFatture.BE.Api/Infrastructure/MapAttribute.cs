namespace PortaleFatture.BE.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Method)]
public class MapAttribute : Attribute
{
    public MapAttribute(MapMethod method, string? route = null)
    {
        Method = method;
        Route = route;
    }

    public MapMethod Method { get; }

    public string? Route { get; }
}

public enum MapMethod
{
    Get,
    Post,
    Put,
    Delete,
    Patch
}