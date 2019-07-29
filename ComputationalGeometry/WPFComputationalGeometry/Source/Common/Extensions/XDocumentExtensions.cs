using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class XDocumentExtensions
    {
        public static string ToStringWithDeclaration(this XDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
    }
}
