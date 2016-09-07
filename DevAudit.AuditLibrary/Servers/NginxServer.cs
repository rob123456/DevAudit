﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class NginxServer : ApplicationServer
    {
        #region Overriden properties
        public override string ServerId { get { return "nginx"; } }

        public override string ServerLabel { get { return "Nginx"; } }

        public override string ApplicationId { get { return "nginx"; } }

        public override string ApplicationLabel { get { return "Nginx"; } }

        public override  Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>() {};

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>() {};

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel { get { return "Nginx"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string DefaultConfigurationFile { get; } = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? Path.Combine(DIR, "etc", "nginx", "nginx.conf") : LocateUnderRoot("conf", "nginx.conf"); 
        #endregion

        #region Public properties
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"nginx", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "nginx", this.Version) }}
            };
            this.Modules = m;
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            Nginx nginx = new Nginx(this.ConfigurationFile.FullName);
            if (nginx.ParseSucceded)
            {
                this.Configuration = nginx;
            }
            return this.Configuration;
        }

        public override string GetVersion()
        {
            HostEnvironment.ProcessStatus process_status;
            string process_output;
            string process_error;
            HostEnvironment.Execute(ApplicationBinary.FullName, "-v", out process_status, out process_output, out process_error);
            if (process_status == HostEnvironment.ProcessStatus.Success && (process_output.Contains("nginx version: ") || process_error.Contains("nginx version: ")))
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Substring("nginx version: ".Length);
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully or could not parse output. Process output: {1}.\nProcess error: {2}.", ApplicationBinary.Name, process_output, process_error));
            }
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.Modules["nginx"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constructors
        public NginxServer(Dictionary<string, object> server_options) : base(server_options)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["nginx"] = this.ApplicationBinary;
            }
            else
            {
                string fn = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePathsUnderRoot("nginx") : CombinePathsUnderRoot("nginx.exe");
                if (!File.Exists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for Nginx was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = new FileInfo(fn);
                    this.ApplicationFileSystemMap["nginx"] = this.ApplicationBinary;
                }
            }
        }
        #endregion
    }
}