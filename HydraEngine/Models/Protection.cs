using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Models
{
    public abstract class Protection
    {
        /// <summary>
		/// Gets the identifier of component used by users.
		/// </summary>
		/// <value>The identifier of component.</value>
		public  string Id { get; }

        /// <summary>
        /// Gets the name of component.
        /// </summary>
        /// <value>The name of component.</value>
        public  string Name { get; }

        /// <summary>
        /// Gets the name of component.
        /// </summary>
        /// <value>The name of component.</value>
        public string ExitMethod { get; set; } = "message";

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public  string Description { get; }

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public bool CompatibleWithMap { get; set; } = true;

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public bool IsNetCoreApp { get; set; } = false;

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public bool ManualReload { get; set; } = false;
        public MemoryStream TempModule { get; set; } = null;

        /// <summary>
        /// Gets the preset this protection is in.
        /// </summary>
        /// <value>The protection's preset.</value>
        public abstract  Task<bool> Execute(ModuleDefMD module);

        /// <summary>
        /// Gets the preset this protection is in.
        /// </summary>
        /// <value>The protection's preset.</value>
        public abstract Task<bool> Execute(string assembly);

        public Exception Errors { get; set; } = new Exception("Undefined");

        public Protection(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

    }
}
