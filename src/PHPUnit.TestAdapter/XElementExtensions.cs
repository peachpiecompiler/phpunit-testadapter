using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// LINQ to XML utility methods.
    /// </summary>
    internal static class XElementExtensions
    {
        /// <summary>
        /// Retrieve the child element of the given name if it exists, or add an empty new one if it doesn't exist.
        /// </summary>
        public static XElement GetOrCreateElement(this XElement parent, XName elementName)
        {
            var element = parent.Element(elementName);
            if (element == null)
            {
                element = new XElement(elementName);
                parent.Add(element);
            }

            return element;
        }
    }
}
