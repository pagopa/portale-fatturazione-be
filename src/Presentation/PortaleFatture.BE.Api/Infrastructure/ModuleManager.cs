using System.Reflection;

namespace PortaleFatture.BE.Api.Infrastructure;

public class ModuleManager
{
    public HashSet<Type> Modules { get; } = new();

    public void AddModule(Type type)
    { 
        if(type is null)
            throw new InvalidDataException($"{nameof(type)} is null");
        if (!type.IsSubclassOf(typeof(Module))) 
            throw new InvalidDataException($"{type.FullName} is not a module"); 

        if (Modules.Contains(type)) 
            return; 

        Modules.Add(type);
    }

    public void AddModulesFromAssembly(Assembly assembly)
    { 
        if (assembly is null)
            throw new InvalidDataException($"{nameof(assembly)} is null");
        var moduleTypes = assembly
            .GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Module)));

        foreach (var moduleType in moduleTypes) 
            AddModule(moduleType); 
    }

    public void RegisterModules(IServiceCollection serviceCollection)
    { 
        if (serviceCollection is null)
            throw new InvalidDataException($"{nameof(serviceCollection)} is null");

        foreach (var module in Modules) 
            serviceCollection.AddScoped(module); 
    }

    public WebApplication MapEndpoints(WebApplication app)
    { 
        if (app is null)
            throw new InvalidDataException($"{nameof(app)} is null");
        using var scope = app.Services.CreateScope();

        foreach (var moduleType in Modules)
        {
            if (moduleType.IsAssignableTo(typeof(IRegistrableModule)))
            {
                var module = (IRegistrableModule)scope.ServiceProvider.GetRequiredService(moduleType);
                module.RegisterEndpoints(app);
                continue;
            } 
            app.Map(moduleType);
        }

        return app;
    }
}