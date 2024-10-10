using System.Reflection;
using System.Runtime.InteropServices;

namespace HeicConvert.Core;

public static class LibHeifSharpDllImportResolver
{
    private static IntPtr cachedLibHeifModule = IntPtr.Zero;
    private static bool firstRequestForLibHeif = true;

    /// <summary>
    /// Registers the <see cref="DllImportResolver"/> for the LibHeifSharp assembly.
    /// </summary>
    public static void Register()
    {
        // The runtime will execute the specified callback when it needs to resolve a native library
        // import for the LibHeifSharp assembly.
        NativeLibrary.SetDllImportResolver(typeof(LibHeifSharp.LibHeifInfo).Assembly, Resolver);
    }

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // We only care about a native library named libheif, the runtime will use
        // its default behavior for any other native library.
        if (string.Equals(libraryName, "libheif", StringComparison.Ordinal))
        {
            // Because the DllImportResolver will be called multiple times we load libheif once
            // and cache the module handle for future requests.
            if (firstRequestForLibHeif)
            {
                firstRequestForLibHeif = false;
                cachedLibHeifModule = LoadNativeLibrary(libraryName, assembly, searchPath);
            }

            return cachedLibHeifModule;
        }

        // Fall back to default import resolver.
        return IntPtr.Zero;
    }

    private static nint LoadNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows the libheif DLL name defaults to heif.dll, so we try to load that if
            // libheif.dll was not found.
            try
            {
                return NativeLibrary.Load(libraryName, assembly, searchPath);
            }
            catch (DllNotFoundException)
            {
                if (NativeLibrary.TryLoad("heif.dll", assembly, searchPath, out IntPtr handle))
                {
                    return handle;
                }
                else
                {
                    throw;
                }
            }
        }
        else if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS())
        {
            // The Apple mobile/embedded platforms statically link libheif into the AOT compiled main program binary.
            return NativeLibrary.GetMainProgramHandle();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // TODO: document this and search other versions
            return NativeLibrary.Load("/opt/homebrew/Cellar/libheif/1.18.2/lib/libheif.dylib", assembly, 0);
        }
        else
        {
            // Use the default runtime behavior for all other platforms.
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }
    }
}