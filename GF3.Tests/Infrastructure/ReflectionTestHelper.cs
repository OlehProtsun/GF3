using System.Reflection;

namespace GF3.Tests.Infrastructure;

internal static class ReflectionTestHelper
{
    public static Assembly LoadAssembly(string assemblyName)
    {
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            var assemblyPath = FindAssemblyPath(assemblyName);
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly '{assemblyName}' could not be located from the test workspace.");
            }

            return Assembly.LoadFrom(assemblyPath);
        }
    }

    public static Type GetType(string assemblyName, string fullTypeName)
        => LoadAssembly(assemblyName).GetType(fullTypeName, throwOnError: true)!;

    public static object? InvokeStatic(Type type, string methodName, params object?[]? arguments)
        => type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?.Invoke(null, arguments);

    public static object Create(Type type, params object?[]? arguments)
        => Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, arguments, culture: null)
           ?? throw new InvalidOperationException($"Could not create instance of {type.FullName}.");

    public static T GetPropertyValue<T>(object instance, string propertyName)
        => (T)(instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(instance)!);

    public static object ParseEnum(Type enumType, string value)
        => Enum.Parse(enumType, value, ignoreCase: false);

    private static string? FindAssemblyPath(string assemblyName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var projectDirectory = Path.Combine(current.FullName, assemblyName);
            if (Directory.Exists(projectDirectory))
            {
                var directCandidate = Directory.EnumerateFiles(projectDirectory, $"{assemblyName}.dll", SearchOption.AllDirectories)
                    .OrderByDescending(path => path.Contains(Path.Combine("bin", "Debug"), StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(path => path.Contains("net10.0-windows", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(directCandidate))
                {
                    return directCandidate;
                }
            }

            current = current.Parent;
        }

        return null;
    }
}
