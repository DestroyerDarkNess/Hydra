using dnlib.DotNet;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Models
{
    public abstract class Pack
    {
        /// <summary>
        /// Gets the identifier of component used by users.
        /// </summary>
        /// <value>The identifier of component.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the name of component.
        /// </summary>
        /// <value>The name of component.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public string Description { get; }

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public AssemblyMap assemblyMap { get; set; }

        /// <summary>
        /// Gets the description of component.
        /// </summary>
        /// <value>The description of component.</value>
        public bool UpdateResurces { get; set; } = false;

        /// <summary>
        /// Gets the preset this protection is in.
        /// </summary>
        /// <value>The protection's preset.</value>
        public abstract Task<bool> Execute(ModuleDefMD module, string Ouput);

        /// <summary>
        /// Gets the preset this protection is in.
        /// </summary>
        /// <value>The protection's preset.</value>
        public abstract Task<bool> Execute(string FilePath , string Ouput);

        public Exception Errors { get; set; } = new Exception("Undefined");

        public Pack(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
