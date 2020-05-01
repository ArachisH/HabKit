using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HabKit.Utilities
{
    public static class KResources
    {
        public static string ReadEmbeddedData(string embeddedFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName) 
                ?? throw new InvalidOperationException("Could not load manifest resource stream.");
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
