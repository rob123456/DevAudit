﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Sprache;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class NuGetv2PackageSource : PackageSource, IDeveloperPackageSource
    {
        #region Constructors
        public NuGetv2PackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
              
        }
        #endregion
        
        #region Overriden members
        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return "NuGet"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "packages.config"; } }
        
        public override IEnumerable<Package> GetPackages(params string[] o) ////Get NuGet packages from reading packages.config
        {
            try
            {
                AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                string xml = config_file.ReadAsText();
                if (xml.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
                {
                    var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                    xml = xml.Remove(0, lastIndexOfUtf8);
                }
                XElement root = XElement.Parse(xml);
                IEnumerable<Package> packages;

                if (root.Name == "Project")
                {
                    // dotnet core csproj file
                    packages = root.Descendants().Where(x => x.Name == "PackageReference").Select(r =>
                        new Package("nuget", r.Attribute("Include").Value, r.Attribute("Version").Value)).ToList();
                }
                else
                {
                    packages =
                        from el in root.Elements("package")
                        select new Package("nuget", el.Attribute("id").Value, el.Attribute("version").Value, "");
                }
                return packages;
            }
            catch (XmlException e)
            {
                throw new Exception("XML exception thrown parsing file: " + this.PackageManagerConfigurationFile, e);
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception thrown attempting to get packages from file: "
                    + this.PackageManagerConfigurationFile, e);
            }

        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = NuGetv2.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;           
        }
        #endregion

        #region Properties
        public string PackageSourceLockFile { get; set; }

        public string DefaultPackageSourceLockFile {get; } = null;
        #endregion
    
        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = NuGetv2.Grammar.Range.Parse(version);
            if (lcs.Count > 1) 
            {
                return true;
            }
            else if (lcs.Count == 1)
            {
                var cs = lcs.Single();
                if (cs.Operator == ExpressionType.Equal)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else throw new ArgumentException($"Failed to parser {version} as a version.");
        }
        #endregion

    }
}
