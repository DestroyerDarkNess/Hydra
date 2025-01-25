using System;
using System.Collections.Generic;
using dnlib.DotNet;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class Renamer : Models.Protection
    {
        private readonly Dictionary<string, string> nameMap = new Dictionary<string, string>();
        public int next = 5;


        public Renamer() : base("Protection.Renamer", "Renamer Phase", "Description for Renamer Phase") {}


        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                //// Crear una instancia de ConfuserProject
                //var project = new ConfuserProject();

                //// Configurar las propiedades del proyecto
                //project.OutputDirectory = Path.Combine(Environment.CurrentDirectory, "output");
                //project.BaseDirectory = Environment.CurrentDirectory;

                //// Agregar un módulo (el archivo que quieres proteger)
                //var module = new ProjectModule();
                //module.Path = "YourAssembly.dll"; // Ruta del ensamblado que quieres proteger
                //project.Add(module);

                //// Crear una instancia de ConfuserParameters
                //var parameters = new ConfuserParameters();
                //parameters.Project = project;
                ////parameters.Logger = new ConsoleLogger(); // Implementa ILogger para manejar la salida de logs

                //// Crear un contexto de Confuser
                //var context = new ConfuserContext();
                ////context.Project = project;

                //// Registrar la protección personalizada
                //var nameProtection = new NameProtection();
                ////nameProtection.Initialize(context);
                //context.Registry.RegisterService("Ki.Rename", typeof(INameService), new NameService(context));

                //// Agregar la protección al pipeline
                //var pipeline = new ProtectionPipeline();
                ////nameProtection.PopulatePipeline(pipeline);
                //context.Pipeline = pipeline;
                //pipeline.InsertPostStage(PipelineStage.Inspection, new AnalyzePhase(nameProtection));
                //pipeline.InsertPostStage(PipelineStage.BeginModule, new RenamePhase(nameProtection));
                //pipeline.InsertPreStage(PipelineStage.EndModule, new PostRenamePhase(nameProtection));
                //pipeline.InsertPostStage(PipelineStage.SaveModules, new NameProtection.ExportMapPhase(nameProtection));

                //// Ejecutar el pipeline
                //pipeline.Execute(context);

                Process(Module);


                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }


        private string ToString(int id)
        {
            return id.ToString("x");
        }

        private string NewName(string name)
        {
            string newName;
            if (!nameMap.TryGetValue(name, out newName))
            {
                nameMap[name] = newName = HydraEngine.Core.Randomizer.GenerateRandomString();
            }
            return newName;
        } //you need to change this method, i use NameService so i don't have to do the renamer method again

        public void Process(ModuleDef module)
        {
            foreach (var type in module.GetTypes())
            {
                if (!type.IsPublic)
                {
                    type.Namespace = NewName(type.Namespace); // you can add this if u want to get namespace seperation on koivm classes
                    type.Name = NewName(type.Name);
                }
                foreach (var genParam in type.GenericParameters)
                    genParam.Name = "";

                var isDelegate = type.BaseType != null &&
                                 (type.BaseType.FullName == "System.Delegate" ||
                                  type.BaseType.FullName == "System.MulticastDelegate");

                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                        foreach (var instr in method.Body.Instructions)
                        {
                            var memberRef = instr.Operand as MemberRef;
                            if (memberRef != null)
                            {
                                var typeDef = memberRef.DeclaringType.ResolveTypeDef();

                                if (memberRef.IsMethodRef && typeDef != null)
                                {
                                    var target = typeDef.ResolveMethod(memberRef);
                                    if (target != null && target.IsRuntimeSpecialName)
                                        typeDef = null;
                                }

                                if (typeDef != null && typeDef.Module == module)
                                    memberRef.Name = NewName(memberRef.Name);
                            }
                        }

                    foreach (var arg in method.Parameters)
                        arg.Name = "";
                    if (method.IsRuntimeSpecialName || isDelegate || type.IsPublic)
                        continue;
                    method.Name = NewName(method.Name);
                    method.CustomAttributes.Clear();
                }
                for (var i = 0; i < type.Fields.Count; i++)
                {
                    var field = type.Fields[i];
                    if (field.IsLiteral)
                    {
                        type.Fields.RemoveAt(i--);
                        continue;
                    }
                    if (field.IsRuntimeSpecialName)
                        continue;
                    field.Name = NewName(field.Name);
                }
                type.Properties.Clear();
                type.Events.Clear();
                type.CustomAttributes.Clear();
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
