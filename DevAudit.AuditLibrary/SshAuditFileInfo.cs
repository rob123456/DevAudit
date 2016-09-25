﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class SshAuditFileInfo : AuditFileInfo
    {
        #region Overriden properties
        public override bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override bool Exists
        {
            get
            {
                return this.AuditEnvironment.FileExists(this.FullName);
            }
        }

        public override long Length
        {
            get
            {
                if (!_Length.HasValue)
                {
                    string c = this.ReadAsText();
                    if (!string.IsNullOrEmpty(c))
                    {
                        this._Length = c.Length;
                    }
                    else
                    {
                        EnvironmentCommandError("Could not read {0} as text.", this.FullName);
                    }
                }
                return this._Length.HasValue ? this._Length.Value : -1;
               
            }
        }

        public override DateTime LastWriteTimeUtc
        {
            get
            {
                DateTime result;
                string cmd = "stat";
                string args = string.Format("-c '%y' {0}", this.FullName);
                string d = EnvironmentExecute(cmd, args);
                if (DateTime.TryParse(d, out result))
                {
                    return result;
                }
                else
                {
                    EnvironmentCommandError("Could not parse result of {0} as DateTime: {1}.", cmd + " " + args, d);
                    return new DateTime(1000, 1, 1);
                }
            }
        }

        public override IDirectoryInfo Directory
        {
            get
            {
                if (this._Directory == null)
                {
                    string o = this.EnvironmentExecute("dirname", this.FullName);
                    if (!string.IsNullOrEmpty(o))
                    {
                        this._Directory = new SshAuditDirectoryInfo(this.SshAuditEnvironment, o);
                    }
                    else
                    {
                        EnvironmentCommandError("Could not get parent directory for {0}.", this.FullName);
                    }
                }
                return this._Directory;
            }
        }

        public override string DirectoryName
        {
            get
            {
                if (this.Directory != null)
                {
                    return this.Directory.Name;
                }
                else
                {
                    EnvironmentCommandError("Could not get directory name for {0}.", this.FullName);
                    return string.Empty;
                }
            }
        }
        #endregion

        #region Overriden methods
        public override IFileInfo Create(string file_path)
        {
            return new SshAuditFileInfo(this.SshAuditEnvironment , file_path);
        }

        public override bool PathExists(string file_path)
        {
            string result = this.EnvironmentExecute("test", "-f " + file_path + " && echo true || echo false");
            if (result == "true" || result == "false")
            {
                return Convert.ToBoolean(result);
            }
            else
            {
                EnvironmentCommandError("Could not test for existence of regular file {0}.", file_path);
                return false;
            }
        }

        public override string ReadAsText()
        {
            string o = this.EnvironmentExecute("cat", this.FullName);
            if (!string.IsNullOrEmpty(o))
            {
                this.AuditEnvironment.Debug("Read {0} characters from file {1}.", o.Length, this.FullName);
                return o;
            }
            else
            {
                EnvironmentCommandError("Could not read as text {0}.", this.FullName);
                return null;
            }
        }
        #endregion

        #region Constructors
        public SshAuditFileInfo(SshAuditEnvironment env, string file_path) : base(env, file_path)
        {
            this.SshAuditEnvironment = env;
        }
        #endregion

        #region Protected properties
        protected SshAuditEnvironment SshAuditEnvironment { get; set; }
        #endregion

        #region Private fields
        private long? _Length;
        private IDirectoryInfo _Directory;
        #endregion
    }
}
