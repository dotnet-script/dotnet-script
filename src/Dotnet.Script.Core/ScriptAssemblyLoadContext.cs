#if NET

using System;
using System.Reflection;
using System.Runtime.Loader;

#nullable enable

namespace Dotnet.Script.Core
{
    /// <summary>
    /// Represents assembly load context for a script with full and automatic assembly isolation.
    /// </summary>
    public class ScriptAssemblyLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAssemblyLoadContext"/> class.
        /// </summary>
        public ScriptAssemblyLoadContext()
        {
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAssemblyLoadContext"/> class
        /// with a name and a value that indicates whether unloading is enabled.
        /// </summary>
        /// <param name="name"><inheritdoc/></param>
        /// <param name="isCollectible"><inheritdoc/></param>
        public ScriptAssemblyLoadContext(string? name, bool isCollectible = false) :
            base(name, isCollectible)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAssemblyLoadContext"/> class
        /// with a value that indicates whether unloading is enabled.
        /// </summary>
        /// <param name="isCollectible"><inheritdoc/></param>
        protected ScriptAssemblyLoadContext(bool isCollectible) :
            base(isCollectible)
        {
        }
#endif

        /// <summary>
        /// <para>
        /// Gets the value indicating whether a specified assembly is homogeneous.
        /// </para>
        /// <para>
        /// Homogeneous assemblies are those shared by both host and scripts.
        /// </para>
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns><c>true</c> if the specified assembly is homogeneous; otherwise, <c>false</c>.</returns>
        protected internal virtual bool IsHomogeneousAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return
                string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Microsoft.CodeAnalysis.Scripting", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName) => InvokeLoading(assemblyName);

        /// <inheritdoc/>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) => InvokeLoadingUnmanagedDll(unmanagedDllName);

        /// <summary>
        /// Provides data for the <see cref="Loading"/> event.
        /// </summary>
        internal sealed class LoadingEventArgs : EventArgs
        {
            public LoadingEventArgs(AssemblyName assemblyName)
            {
                Name = assemblyName;
            }

            public AssemblyName Name { get; }
        }

        /// <summary>
        /// Represents a method that handles the <see cref="Loading"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The loaded assembly or <c>null</c> if the assembly cannot be resolved.</returns>
        internal delegate Assembly? LoadingEventHandler(ScriptAssemblyLoadContext sender, LoadingEventArgs args);

        LoadingEventHandler? m_Loading;

        /// <summary>
        /// Occurs when an assembly is being loaded.
        /// </summary>
        internal event LoadingEventHandler Loading
        {
            add => m_Loading += value;
            remove => m_Loading -= value;
        }

        Assembly? InvokeLoading(AssemblyName assemblyName)
        {
            var eh = m_Loading;
            if (eh != null)
            {
                var args = new LoadingEventArgs(assemblyName);
                foreach (LoadingEventHandler handler in eh.GetInvocationList())
                {
                    var assembly = handler(this, args);
                    if (assembly != null)
                        return assembly;
                }
            }
            return null;
        }

        /// <summary>
        /// Provides data for the <see cref="LoadingUnmanagedDll"/> event.
        /// </summary>
        internal sealed class LoadingUnmanagedDllEventArgs : EventArgs
        {
            public LoadingUnmanagedDllEventArgs(string unmanagedDllName, Func<string, IntPtr> loadUnmanagedDllFromPath)
            {
                UnmanagedDllName = unmanagedDllName;
                LoadUnmanagedDllFromPath = loadUnmanagedDllFromPath;
            }

            public string UnmanagedDllName { get; }
            public Func<string, IntPtr> LoadUnmanagedDllFromPath { get; }
        }

        /// <summary>
        /// Represents a method that handles the <see cref="LoadingUnmanagedDll"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The loaded DLL or <see cref="IntPtr.Zero"/> if the DLL cannot be resolved.</returns>
        internal delegate IntPtr LoadingUnmanagedDllEventHandler(ScriptAssemblyLoadContext sender, LoadingUnmanagedDllEventArgs args);

        LoadingUnmanagedDllEventHandler? m_LoadingUnmanagedDll;

        /// <summary>
        /// Occurs when an unmanaged DLL is being loaded.
        /// </summary>
        internal event LoadingUnmanagedDllEventHandler LoadingUnmanagedDll
        {
            add => m_LoadingUnmanagedDll += value;
            remove => m_LoadingUnmanagedDll -= value;
        }

        IntPtr InvokeLoadingUnmanagedDll(string unmanagedDllName)
        {
            var eh = m_LoadingUnmanagedDll;
            if (eh != null)
            {
                var args = new LoadingUnmanagedDllEventArgs(unmanagedDllName, LoadUnmanagedDllFromPath);
                foreach (LoadingUnmanagedDllEventHandler handler in eh.GetInvocationList())
                {
                    var dll = handler(this, args);
                    if (dll != IntPtr.Zero)
                        return dll;
                }
            }
            return IntPtr.Zero;
        }
    }
}

#endif
