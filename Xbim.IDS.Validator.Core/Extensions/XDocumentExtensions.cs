using IdsLib.IdsSchema.IdsNodes;
using System.Xml;
using System.Xml.Linq;

namespace Xbim.IDS.Validator.Core.Extensions
{
    public static class XDocumentExtensions
    {
        /// <summary>
        /// Reads the IDS version from the given Xml <see cref="XDocument"/>
        /// </summary>
        /// <param name="document"></param>
        /// <returns>The <see cref="IdsVersion"/></returns>
        public static IdsVersion ReadIdsVersion(this XDocument document)
        {
            return GetVersionFromLocation(document.ReadIdsSchemaLocation());
        }

        /// <summary>
        /// Reads the Xml Schema Location from the given Xml <see cref="XDocument"/>
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A string representing the Xml schema location</returns>
        public static string ReadIdsSchemaLocation(this XDocument document)
        {
            var reader = document.CreateReader();
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.LocalName)
                        {
                            case "ids":
                                return reader.GetAttribute("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance") ?? string.Empty;
                                
                            default:
                                break;
                        }
                        break;
                }
            }
            return string.Empty;
        }

        // from IdsLib.IdsFacts
        internal static IdsVersion GetVersionFromLocation(string location)
        {
            return location switch
            {
                // the following are the only canonical versions accepted
                "http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/0.9.6/ids.xsd" => IdsVersion.Ids0_9_6,
                "http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/0.9.7/ids.xsd" => IdsVersion.Ids0_9_7,
                "http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/1.0/ids.xsd" => IdsVersion.Ids1_0,
                _ => IdsVersion.Invalid,
            };
        }
    }
}
